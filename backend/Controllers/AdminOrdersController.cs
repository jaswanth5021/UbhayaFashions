using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(ApplicationDbContext db, IDelhiveryService delhivery, IOrderConfirmationEmailService orderEmail, ILogger<AdminOrdersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedDate)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                Customer = order.Customer == null ? null : new
                {
                    order.Customer.Id,
                    order.Customer.Name,
                    order.Customer.Email,
                    order.Customer.Mobile,
                    order.Customer.DateOfBirth
                },
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                ShippingStatus = db.DelhiveryShipments.Where(s => s.OrderId == order.Id).Select(s => s.ShipmentStatus).FirstOrDefault(),
                ShippingWaybill = db.DelhiveryShipments.Where(s => s.OrderId == order.Id).Select(s => s.Waybill).FirstOrDefault(),
                Items = order.Items.Select(item => new
                {
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                Customer = order.Customer == null ? null : new
                {
                    order.Customer.Id,
                    order.Customer.Name,
                    order.Customer.Email,
                    order.Customer.Mobile,
                    order.Customer.DateOfBirth
                },
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                Items = order.Items.Select(item => new
                {
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.Include(x => x.Items).Include(x => x.Customer).Include(x => x.DelhiveryShipment)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return NotFound("Order not found.");
        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) return Conflict("This order has already been cancelled.");
        var orderStatus = order.Status ?? string.Empty;
        if (new[] { "deliver", "complete", "transit", "pickup", "dispatch", "ship" }.Any(term => orderStatus.Contains(term, StringComparison.OrdinalIgnoreCase)))
            return Conflict("This order cannot be cancelled automatically because fulfilment has progressed.");
        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var variants = await db.ProductVariants.Where(v => productIds.Contains(v.ProductId)).ToListAsync(cancellationToken);
        static string Normalize(string? value) => (value ?? string.Empty).Trim();
        var restorations = new List<(ProductVariant Variant, OrderItem Item)>();
        foreach (var item in order.Items)
        {
            var matches = variants.Where(v => v.ProductId == item.ProductId && string.Equals(Normalize(v.Size), Normalize(item.Size), StringComparison.OrdinalIgnoreCase)).ToList();
            var exact = matches.Where(v => string.Equals(Normalize(v.Color), Normalize(item.Color), StringComparison.OrdinalIgnoreCase)).ToList();
            var variant = exact.Count == 1 ? exact[0] : matches.Count == 1 ? matches[0] : null;
            if (variant is null) return Conflict($"Unable to identify inventory variant for {item.ProductName} ({item.Size}, {item.Color}). No stock was changed.");
            restorations.Add((variant, item));
        }
        var shipment = order.DelhiveryShipment;
        if (shipment is not null)
        {
            var shipmentStatus = shipment.ShipmentStatus?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(shipment.Waybill))
            {
                var allowed = new[] { "Manifested", "Pending", "Open", "Scheduled" };
                if (!allowed.Any(value => string.Equals(value, shipmentStatus, StringComparison.OrdinalIgnoreCase)))
                    return Conflict($"Delhivery shipment status is '{shipmentStatus}'. Automatic cancellation is allowed only before courier handover.");
                try { await delhivery.CancelShipmentAsync(shipment.Waybill, cancellationToken); }
                catch (Exception ex) { logger.LogError(ex, "Delhivery cancellation failed for order {OrderId}, AWB {Waybill}.", id, shipment.Waybill); return Conflict("Delhivery did not confirm cancellation. Order and inventory were left unchanged. " + ex.Message); }
            }
            else if (!string.IsNullOrWhiteSpace(shipmentStatus) && !new[] { "NotCreated", "Creation Failed", "Failed" }.Contains(shipmentStatus, StringComparer.OrdinalIgnoreCase))
                return Conflict($"Shipment status '{shipmentStatus}' cannot be cancelled automatically.");
        }
        foreach (var (variant, item) in restorations)
        {
            // This admin workflow is specifically for damaged items discovered during packing.
            // The ordered quantity was already deducted at checkout; do not add it back to sellable stock.
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                Size = item.Size,
                Type = "Damaged",
                Quantity = 0,
                StockAfter = variant.Stock,
                Note = $"Order #{order.Id} cancelled by admin: {item.Quantity} unit(s) marked damaged and excluded from sellable inventory."
            });
        }
        order.Status = "Cancelled";
        if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)) order.PaymentStatus = "Refund Pending";
        if (shipment is not null) { shipment.ShipmentStatus = "Cancelled"; shipment.UpdatedAtUtc = DateTime.UtcNow; shipment.LastError = null; }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await orderEmail.SendOrderCancellationAsync(order, order.Customer);
        return Ok(new { success = true, orderId = order.Id, order.Status, order.PaymentStatus, message = "Order cancelled. Damaged units remain excluded from sellable stock and are recorded in inventory transactions. Process any Refund Pending payment separately in Razorpay." });
    }
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> Status(int id, AdminOrderStatusRequest request)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return Conflict("A cancelled order cannot be moved back to an active status.");

        order.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            order.PaymentStatus = request.PaymentStatus;

        await db.SaveChangesAsync();

        return Ok(new { order.Id, order.Status, order.PaymentStatus });
    }
}
