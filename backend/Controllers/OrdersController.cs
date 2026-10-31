using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(ApplicationDbContext db, IOrderConfirmationEmailService orderEmail, IDelhiveryService delhivery, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return Unauthorized();
        if (request.Items is null || request.Items.Count == 0) return BadRequest("Cart is empty.");
        if (request.Items.Any(item => item.Quantity <= 0 || string.IsNullOrWhiteSpace(item.Size))) return BadRequest("Choose a size and a valid quantity for each item.");

        var ids = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Products.Include(product => product.Variants).Where(product => ids.Contains(product.Id)).ToListAsync();
        if (products.Count != ids.Count) return BadRequest("One or more products no longer exist.");

        decimal total = 0;
        var inventoryTransactions = new List<InventoryTransaction>();
        var order = new Order { CustomerId = customerId, ShippingAddress = request.ShippingAddress, PaymentMethod = "COD" };

        foreach (var item in request.Items)
        {
            var product = products.Single(product => product.Id == item.ProductId);
            var hasColorVariants = product.Variants.Any(variant => !string.IsNullOrWhiteSpace(variant.Color));
            var variant = hasColorVariants
                ? product.Variants.FirstOrDefault(variant => variant.Size == item.Size && string.Equals(variant.Color, item.Color, StringComparison.OrdinalIgnoreCase))
                : product.Variants.FirstOrDefault(variant => variant.Size == item.Size);
            if (variant is null) return BadRequest($"{product.Name} does not have size {item.Size}.");
            if (variant.Stock < item.Quantity) return BadRequest($"{product.Name} ({variant.Size}) has insufficient stock.");

            total += variant.Price * item.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = variant.Price,
                Quantity = item.Quantity,
                Size = variant.Size,
                Color = item.Color
            });
            variant.Stock -= item.Quantity;
            inventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                Size = variant.Size,
                Type = "Sale",
                Quantity = -item.Quantity,
                StockAfter = variant.Stock,
                Note = "Customer order"
            });
        }

        order.TotalAmount = total;
        db.Orders.Add(order);
        db.InventoryTransactions.AddRange(inventoryTransactions);
        await db.SaveChangesAsync();
        await orderEmail.SendOrderConfirmationAsync(order, customer);
        return Ok(order);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        var order = await db.Orders
            .Include(x => x.Items)
            .Include(x => x.DelhiveryShipment)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (order is null) return NotFound("Order not found.");

        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return Conflict("This order has already been cancelled.");

        var orderStatus = order.Status ?? string.Empty;
        if (orderStatus.Contains("deliver", StringComparison.OrdinalIgnoreCase) ||
            orderStatus.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
            orderStatus.Contains("transit", StringComparison.OrdinalIgnoreCase) ||
            orderStatus.Contains("pickup", StringComparison.OrdinalIgnoreCase) ||
            orderStatus.Contains("dispatch", StringComparison.OrdinalIgnoreCase) ||
            orderStatus.Contains("ship", StringComparison.OrdinalIgnoreCase))
            return Conflict("This order can no longer be cancelled because fulfilment has progressed.");

        var shipment = order.DelhiveryShipment;
        if (shipment is not null)
        {
            var shipmentStatus = shipment.ShipmentStatus?.Trim() ?? string.Empty;
            var canCancelBeforeShipment = string.IsNullOrWhiteSpace(shipment.Waybill) &&
                (string.IsNullOrWhiteSpace(shipmentStatus) ||
                 shipmentStatus.Equals("NotCreated", StringComparison.OrdinalIgnoreCase) ||
                 shipmentStatus.Equals("Creation Failed", StringComparison.OrdinalIgnoreCase) ||
                 shipmentStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase));

            if (!canCancelBeforeShipment)
                return Conflict("This order can only be cancelled before the Delhivery shipment is created. Current shipment status: " + shipmentStatus);

            shipment.ShipmentStatus = "Cancelled";
            shipment.UpdatedAtUtc = DateTime.UtcNow;
        }

        // Validate every stock variant before changing any inventory.
        var stockRestorations = new List<(ProductVariant Variant, OrderItem Item)>();
        foreach (var item in order.Items)
        {
            var variant = await db.ProductVariants.FirstOrDefaultAsync(x =>
                x.ProductId == item.ProductId &&
                x.Size == item.Size &&
                (string.IsNullOrWhiteSpace(item.Color)
                    ? string.IsNullOrWhiteSpace(x.Color)
                    : x.Color == item.Color), cancellationToken);

            if (variant is null)
                return Conflict($"Unable to restore inventory for {item.ProductName} ({item.Size}). Please contact support.");

            stockRestorations.Add((variant, item));
        }

        foreach (var (variant, item) in stockRestorations)
        {
            variant.Stock += item.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                Size = item.Size,
                Type = "Cancellation",
                Quantity = item.Quantity,
                StockAfter = variant.Stock,
                Note = $"Customer cancelled order #{order.Id} before shipment creation"
            });
        }

        order.Status = "Cancelled";
        if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            order.PaymentStatus = "Refund Pending";

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = string.Equals(order.PaymentStatus, "Refund Pending", StringComparison.OrdinalIgnoreCase)
                ? "Your order has been cancelled. Your payment is marked for refund processing."
                : "Your order has been cancelled successfully.",
            orderId = order.Id,
            order.Status,
            order.PaymentStatus
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyOrders()
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await db.Orders.AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedDate)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                order.RazorpayOrderId,
                ShippingStatus = db.DelhiveryShipments.Where(s => s.OrderId == order.Id).Select(s => s.ShipmentStatus).FirstOrDefault(),
                ShippingWaybill = db.DelhiveryShipments.Where(s => s.OrderId == order.Id).Select(s => s.Waybill).FirstOrDefault(),
                Items = order.Items.Select(item => new
                {
                    item.Id,
                    item.OrderId,
                    item.ProductId,
                    item.ProductName,
                    ImageUrl = db.Products
                        .Where(product => product.Id == item.ProductId)
                        .Select(product => product.ImageUrl)
                        .FirstOrDefault(),
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                })
            })
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id:int}/tracking")]
    public async Task<IActionResult> TrackOrder(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        var order = await db.Orders.AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new
            {
                x.Id, x.Status, x.PaymentStatus, x.PaymentMethod, x.CreatedDate,
                Shipment = db.DelhiveryShipments.Where(s => s.OrderId == x.Id)
                    .Select(s => new { s.Id, s.Waybill, s.ShipmentStatus, s.UpdatedAtUtc })
                    .FirstOrDefault()
            }).FirstOrDefaultAsync(cancellationToken);

        if (order is null) return NotFound();

        if (order.Shipment is not null &&
            !string.IsNullOrWhiteSpace(order.Shipment.Waybill) &&
            (!order.Shipment.UpdatedAtUtc.HasValue ||
             order.Shipment.UpdatedAtUtc.Value < DateTime.UtcNow.AddMinutes(-2)))
        {
            try
            {
                var status = await delhivery.RefreshTrackingAsync(order.Shipment.Waybill, cancellationToken);
                var shipment = await db.DelhiveryShipments.FirstOrDefaultAsync(
                    x => x.Id == order.Shipment.Id, cancellationToken);
                if (shipment is not null && !string.IsNullOrWhiteSpace(status))
                {
                    shipment.ShipmentStatus = status;
                    shipment.UpdatedAtUtc = DateTime.UtcNow;
                    shipment.LastError = null;
                    await db.SaveChangesAsync(cancellationToken);
                    order = new
                    {
                        order.Id, order.Status, order.PaymentStatus, order.PaymentMethod, order.CreatedDate,
                        Shipment = new { shipment.Id, shipment.Waybill, shipment.ShipmentStatus, shipment.UpdatedAtUtc }
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Delhivery tracking refresh failed for order {OrderId}", id);
            }
        }

        var details = await db.Orders.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.TotalAmount,
                x.ShippingAddress,
                CustomerName = x.Customer.Name,
                CustomerMobile = x.Customer.Mobile,
                ItemCount = x.Items.Sum(item => (int?)item.Quantity) ?? 0,
                FirstItemName = x.Items.OrderBy(item => item.Id).Select(item => item.ProductName).FirstOrDefault(),
                FirstItemImage = x.Items.OrderBy(item => item.Id)
                    .Select(item => db.Products.Where(p => p.Id == item.ProductId).Select(p => p.ImageUrl).FirstOrDefault())
                    .FirstOrDefault(),
                Shipment = db.DelhiveryShipments.Where(s => s.OrderId == x.Id)
                    .Select(s => new
                    {
                        s.RecipientName, s.RecipientPhone, s.Address, s.City, s.State, s.PostalCode,
                        s.PickupLocation, s.CreatedAtUtc
                    })
                    .FirstOrDefault()
            })
            .FirstAsync(cancellationToken);

        var shipmentInfo = details.Shipment;
        var hasShipmentAddress = shipmentInfo is not null && !string.IsNullOrWhiteSpace(shipmentInfo.Address);

        return Ok(new
        {
            orderId = order.Id,
            orderStatus = order.Status,
            paymentStatus = order.PaymentStatus,
            paymentMethod = order.PaymentMethod,
            orderDate = order.CreatedDate,
            shippingStatus = order.Shipment?.ShipmentStatus ?? "NotCreated",
            waybill = order.Shipment?.Waybill,
            lastTrackingUpdateUtc = order.Shipment?.UpdatedAtUtc,
            shipmentCreatedUtc = shipmentInfo?.CreatedAtUtc,
            pickupLocation = shipmentInfo?.PickupLocation,
            totalAmount = details.TotalAmount,
            itemCount = details.ItemCount,
            firstItemName = details.FirstItemName,
            firstItemImageUrl = details.FirstItemImage,
            recipientName = hasShipmentAddress && !string.IsNullOrWhiteSpace(shipmentInfo!.RecipientName)
                ? shipmentInfo.RecipientName : details.CustomerName,
            recipientPhone = hasShipmentAddress && !string.IsNullOrWhiteSpace(shipmentInfo!.RecipientPhone)
                ? shipmentInfo.RecipientPhone : details.CustomerMobile,
            deliveryAddress = hasShipmentAddress
                ? $"{shipmentInfo!.Address}, {shipmentInfo.City}, {shipmentInfo.State} - {shipmentInfo.PostalCode}"
                : details.ShippingAddress
        });
    }
}
