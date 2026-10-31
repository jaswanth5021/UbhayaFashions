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
    [HttpGet("/api/admin/delhivery/serviceability/{postalCode}")]
    public async Task<IActionResult> CheckServiceability(string postalCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length != 6 || !postalCode.All(char.IsDigit))
            return BadRequest(new { error = "Enter a valid six-digit PIN code." });

        try
        {
            var result = await delhivery.CheckPincodeServiceabilityAsync(postalCode, cancellationToken);
            return Ok(new
            {
                result.PostalCode,
                result.IsServiceable,
                result.Prepaid,
                result.Cod,
                result.Pickup,
                environment = configuration["Delhivery:BaseUrl"] ?? "https://staging-express.delhivery.com/"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery pincode serviceability check failed for {PostalCode}", postalCode);
            return StatusCode(502, new { error = ex.Message });
        }
    }

    [HttpGet("/api/admin/delhivery/shipping-cost")]
    public async Task<IActionResult> CalculateShippingCost(
        [FromQuery] string originPin,
        [FromQuery] string destinationPin,
        [FromQuery] int weightGrams = 1000,
        [FromQuery] string shipmentMode = "Surface",
        [FromQuery] string paymentMode = "Pre-paid",
        [FromQuery] string status = "Delivered",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await delhivery.CalculateShippingCostAsync(
                originPin, destinationPin, weightGrams, shipmentMode, paymentMode, status, cancellationToken);
            return Ok(new
            {
                estimated = true,
                note = "Delhivery rates are approximate. Staging may return zero charges; production rates require the live account.",
                result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery shipping cost calculation failed for {OriginPin} to {DestinationPin}", originPin, destinationPin);
            return StatusCode(502, new { error = ex.Message });
        }
    }

    [HttpGet("/api/admin/delhivery/waybills")]
    public async Task<IActionResult> FetchWaybills([FromQuery] int count = 1, CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 10000)
            return BadRequest(new { error = "Count must be between 1 and 10000." });

        try
        {
            var result = await delhivery.FetchWaybillsAsync(count, cancellationToken);
            return Ok(new { count, waybills = result });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery waybill fetch failed for count {Count}", count);
            return StatusCode(502, new { error = ex.Message });
        }
    }

    [HttpGet("/api/admin/delhivery/label/{waybill}")]
    public async Task<IActionResult> DownloadLabel(string waybill, CancellationToken cancellationToken)
    {
        try
        {
            var label = await delhivery.DownloadLabelAsync(waybill, cancellationToken);
            return File(label.Content, label.ContentType, label.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery label download failed for waybill {Waybill}", waybill);
            return StatusCode(502, new { error = ex.Message });
        }
    }

    [HttpPost("{orderId:int}/delhivery-shipment")]
    public async Task<IActionResult> CreateOrRetry(int orderId, CreateDelhiveryShipmentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientName) ||
            string.IsNullOrWhiteSpace(request.RecipientPhone) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) ||
            string.IsNullOrWhiteSpace(request.PostalCode))
            return BadRequest(new { error = "Recipient name, phone, address, city, state and postal code are required." });

        if (request.WeightGrams < 1 || request.WeightGrams > 100000)
            return BadRequest(new { error = "WeightGrams must be between 1 and 100000." });

        var order = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null) return NotFound(new { error = "Order not found." });
        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { error = "A cancelled order cannot be submitted to Delhivery." });
        if (string.Equals(order.PaymentMethod, "Prepaid", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { error = "A prepaid shipment cannot be created before payment is verified." });

        var shipment = await db.DelhiveryShipments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        if (shipment is not null && !string.IsNullOrWhiteSpace(shipment.Waybill))
            return Conflict(new { error = "A Delhivery shipment already exists for this order.", waybill = shipment.Waybill });

        var pickup = configuration["Delhivery:PickupLocationName"];
        if (string.IsNullOrWhiteSpace(pickup))
            return StatusCode(503, new { error = "Delhivery pickup location is not configured." });
        if (string.IsNullOrWhiteSpace(configuration["Delhivery:ClientName"]))
            return StatusCode(503, new { error = "Delhivery:ClientName is not configured. Set the exact client name from Delhivery One." });

        try
        {
            var serviceability = await delhivery.CheckPincodeServiceabilityAsync(request.PostalCode.Trim(), cancellationToken);
            var isCod = string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase);
            var allowed = isCod ? serviceability.Cod : serviceability.Prepaid;
            if (!allowed)
            {
                var mode = isCod ? "COD" : "prepaid";
                return BadRequest(new
                {
                    error = $"Delhivery does not currently support {mode} delivery to PIN code {request.PostalCode}. Shipment was not created.",
                    serviceability
                });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery serviceability check failed before shipment creation for order {OrderId}", orderId);
            return StatusCode(502, new { error = "Could not verify destination PIN-code serviceability. Shipment was not created.", detail = ex.Message });
        }

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
            return Ok(new
            {
                success = true,
                orderId,
                waybill = shipment.Waybill,
                shippingStatus = shipment.ShipmentStatus,
                labelUrl = shipment.LabelUrl,
                labelApiUrl = Url.ActionLink(nameof(DownloadLabel), values: new { waybill = shipment.Waybill })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery shipment creation failed for order {OrderId}", orderId);
            shipment.ShipmentStatus = "Creation Failed";
            shipment.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return StatusCode(502, new { error = ex.Message, orderId, shipmentStatus = shipment.ShipmentStatus });
        }
    }
}
