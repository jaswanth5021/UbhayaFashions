using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/delivery")]
public sealed class DeliveryController(
    IDelhiveryService delhivery,
    ILogger<DeliveryController> logger) : ControllerBase
{
    [HttpGet("serviceability/{postalCode}")]
    public async Task<IActionResult> CheckServiceability(string postalCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length != 6 || !postalCode.All(char.IsDigit))
            return BadRequest(new { success = false, serviceable = false, message = "Enter a valid six-digit PIN code." });

        try
        {
            var result = await delhivery.CheckPincodeServiceabilityAsync(postalCode, cancellationToken);
            // Ubhaya currently accepts prepaid orders only, so COD-only serviceability is not sufficient.
            if (!result.Prepaid)
                return Ok(new
                {
                    success = true,
                    serviceable = false,
                    postalCode,
                    message = $"Sorry, Delhivery does not currently deliver prepaid orders to PIN code {postalCode}. Please try another delivery PIN code."
                });

            return Ok(new
            {
                success = true,
                serviceable = true,
                postalCode,
                message = $"Great! Delivery is available to PIN code {postalCode}."
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Public Delhivery serviceability check failed for PIN {PostalCode}", postalCode);
            return StatusCode(502, new
            {
                success = false,
                serviceable = false,
                message = "We couldn't verify delivery availability right now. Please try again in a few minutes."
            });
        }
    }
}