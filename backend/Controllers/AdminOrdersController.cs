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
public class AdminOrdersController(ApplicationDbContext db, IDelhiveryService delhivery, IOrderConfirmationEmailService orderEmail, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AdminOrdersController> logger) : ControllerBase
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
        var order = await db.Orders.Include(x => x.Items).Include(x => x.Customer).Include(x => x.DelhiveryShipment).Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return NotFound("Order not found.");
        var alreadyCancelled = string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
        var retryRefund = alreadyCancelled && string.Equals(order.PaymentStatus, "Refund Pending", StringComparison.OrdinalIgnoreCase);
        if (alreadyCancelled && !retryRefund) return Conflict("This order has already been cancelled.");

        var shipment = order.DelhiveryShipment;
        if (!alreadyCancelled && shipment is not null && !string.IsNullOrWhiteSpace(shipment.Waybill) && !string.Equals(shipment.ShipmentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            try { await delhivery.CancelShipmentAsync(shipment.Waybill, cancellationToken); shipment.ShipmentStatus = "Cancelled"; shipment.UpdatedAtUtc = DateTime.UtcNow; shipment.LastError = null; }
            catch (Exception ex) { logger.LogError(ex, "Delhivery cancellation failed for order {OrderId}, AWB {Waybill}.", id, shipment.Waybill); return Conflict("Delhivery did not confirm cancellation. Order and refund were not changed. " + ex.Message); }
        }

        var prepaid = string.Equals(order.PaymentMethod, "Prepaid", StringComparison.OrdinalIgnoreCase) || string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) || retryRefund;
        var payment = order.Payments.OrderByDescending(x => x.PaidDate ?? x.CreatedDate).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.RazorpayPaymentId));
        var refundInitiated = false;
        string? refundId = null;
        string? refundWarning = null;
        if (prepaid && payment is not null && !string.Equals(payment.Status, "Refunded", StringComparison.OrdinalIgnoreCase))
        {
            var keyId = configuration["Razorpay:KeyId"]; var keySecret = configuration["Razorpay:KeySecret"];
            if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret)) refundWarning = "Razorpay credentials are not configured; refund remains pending."; 
            else try
            {
                var client = httpClientFactory.CreateClient("Razorpay");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{keyId}:{keySecret}")));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"payments/{Uri.EscapeDataString(payment.RazorpayPaymentId!)}/refund") { Content = System.Net.Http.Json.JsonContent.Create(new { amount = checked((long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero)), receipt = $"ubhaya-order-{order.Id}-refund" }) };
                request.Headers.TryAddWithoutValidation("X-Refund-Idempotency", $"ubhaya-order-{order.Id}-refund");
                using var response = await client.SendAsync(request, cancellationToken); var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Razorpay refund failed ({(int)response.StatusCode}): {body}");
                using var json = System.Text.Json.JsonDocument.Parse(body); var root = json.RootElement;
                refundId = root.TryGetProperty("id", out var rid) ? rid.GetString() : null;
                var state = root.TryGetProperty("status", out var st) ? st.GetString() : null;
                if (string.IsNullOrWhiteSpace(refundId) || !(string.Equals(state, "pending", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "processed", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Razorpay did not confirm the refund request: " + body);
                payment.Status = "Refund Initiated"; refundInitiated = true;
                logger.LogInformation("Razorpay refund initiated for order {OrderId}. RefundId={RefundId}, Status={Status}", id, refundId, state);
            }
            catch (Exception ex) { refundWarning = ex.Message; logger.LogError(ex, "Refund initiation failed for cancelled order {OrderId}.", id); }
        }
        else if (prepaid && payment is null && !string.Equals(order.PaymentStatus, "Refunded", StringComparison.OrdinalIgnoreCase)) refundWarning = "No Razorpay payment reference was found; refund needs manual review."; 

        if (refundInitiated) order.PaymentStatus = "Refund Initiated";

        if (!alreadyCancelled)
        {
            order.Status = "Cancelled";
            order.PaymentStatus = prepaid ? (refundInitiated ? "Refund Initiated" : "Refund Pending") : "Not Required";
            var ids = order.Items.Select(x => x.ProductId).Distinct().ToList();
            var variants = await db.ProductVariants.Where(x => ids.Contains(x.ProductId)).ToListAsync(cancellationToken);
            foreach (var item in order.Items)
            {
                var variant = variants.FirstOrDefault(v => v.ProductId == item.ProductId && string.Equals((v.Size ?? "").Trim(), (item.Size ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && string.Equals((v.Color ?? "").Trim(), (item.Color ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
                if (variant is not null) db.InventoryTransactions.Add(new InventoryTransaction { ProductId = item.ProductId, Size = item.Size, Type = "Damaged", Quantity = 0, StockAfter = variant.Stock, Note = $"Order #{order.Id} cancelled by admin; quantity {item.Quantity} retained outside sellable inventory." });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await orderEmail.SendOrderCancellationAsync(order, order.Customer);
        return Ok(new { success = true, orderId = order.Id, order.Status, order.PaymentStatus, refundInitiated, refundId, refundWarning, message = refundInitiated ? "Order and Delhivery shipment cancelled; Razorpay refund request accepted." : prepaid ? "Order cancelled, but refund still requires attention." : "Order and Delhivery shipment cancelled." });
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
