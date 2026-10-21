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

        return Ok(new
        {
            orderId = order.Id,
            orderStatus = order.Status,
            paymentStatus = order.PaymentStatus,
            paymentMethod = order.PaymentMethod,
            orderDate = order.CreatedDate,
            shippingStatus = order.Shipment?.ShipmentStatus ?? "NotCreated",
            waybill = order.Shipment?.Waybill,
            lastTrackingUpdateUtc = order.Shipment?.UpdatedAtUtc
        });
    }
}
