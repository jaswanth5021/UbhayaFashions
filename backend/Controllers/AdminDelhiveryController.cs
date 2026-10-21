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
public sealed class AdminDelhiveryController(
    ApplicationDbContext db,
    IDelhiveryService delhivery,
    IConfiguration configuration,
    ILogger<AdminDelhiveryController> logger) : ControllerBase
{
    [HttpPost("{orderId:int}/delhivery-shipment")]
    public async Task<IActionResult> CreateOrRetry(int orderId, CreateDelhiveryShipmentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientName) ||
            string.IsNullOrWhiteSpace(request.RecipientPhone) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) ||
            string.IsNullOrWhiteSpace(request.PostalCode))
            return BadRequest("Recipient name, phone, address, city, state and postal code are required.");

        if (request.WeightGrams < 1 || request.WeightGrams > 100000)
            return BadRequest("WeightGrams must be between 1 and 100000.");

        var order = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null) return NotFound("Order not found.");
        if (string.Equals(order.PaymentMethod, "Prepaid", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            return Conflict("A prepaid shipment cannot be created before payment is verified.");

        var shipment = await db.DelhiveryShipments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (shipment is not null && !string.IsNullOrWhiteSpace(shipment.Waybill))
            return Conflict(new { message = "A Delhivery shipment already exists for this order.", waybill = shipment.Waybill });

        var pickup = configuration["Delhivery:PickupLocationName"];
        if (string.IsNullOrWhiteSpace(pickup)) return StatusCode(503, "Delhivery pickup location is not configured.");

        if (shipment is null)
        {
            shipment = new DelhiveryShipment { OrderId = order.Id };
            db.DelhiveryShipments.Add(shipment);
        }

        shipment.PickupLocation = pickup;
        shipment.RecipientName = request.RecipientName.Trim();
        shipment.RecipientPhone = request.RecipientPhone.Trim();
        shipment.Address = request.Address.Trim();
        shipment.City = request.City.Trim();
        shipment.State = request.State.Trim();
        shipment.PostalCode = request.PostalCode.Trim();
        shipment.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        shipment.WeightGrams = request.WeightGrams;
        shipment.ShipmentStatus = "Creating";
        shipment.LastError = null;
        shipment.AttemptCount++;
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var created = await delhivery.CreateShipmentAsync(order, shipment, cancellationToken);
            shipment.Waybill = created.Waybill;
            shipment.LabelUrl = created.LabelUrl;
            shipment.ShipmentStatus = "Shipment Created";
            shipment.LastError = null;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { orderId, waybill = shipment.Waybill, shippingStatus = shipment.ShipmentStatus, labelUrl = shipment.LabelUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery shipment creation failed for order {OrderId}", orderId);
            shipment.ShipmentStatus = "Creation Failed";
            shipment.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return StatusCode(502, "Delhivery shipment creation failed. Check server configuration and retry after correcting the issue.");
        }
    }
}