using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/payment/razorpay")]
public sealed class RazorpayWebhookController(
    ApplicationDbContext db,
    IConfiguration configuration,
    IDelhiveryService delhivery,
    IOrderConfirmationEmailService orderEmail,
    ILogger<RazorpayWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    [Consumes("application/json")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var webhookSecret = configuration["Razorpay:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogCritical("Razorpay webhook received but Razorpay:WebhookSecret is not configured.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        if (!TryReadSignature(signature, out var suppliedSignature))
            return Unauthorized();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest();

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature))
        {
            logger.LogWarning("Rejected Razorpay webhook because its signature was invalid.");
            return Unauthorized();
        }

        var eventId = Request.Headers["x-razorpay-event-id"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length > 100)
        {
            logger.LogWarning("Rejected Razorpay webhook because x-razorpay-event-id was missing or invalid.");
            return BadRequest("Missing or invalid Razorpay event ID.");
        }

        string eventType;
        string? razorpayPaymentId;
        string? razorpayOrderId;
        long amountInPaise;
        string? currency;
        string? paymentStatus;
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            eventType = GetString(root, "event") ?? "";
            var entity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            razorpayPaymentId = GetString(entity, "id");
            razorpayOrderId = GetString(entity, "order_id");
            amountInPaise = entity.TryGetProperty("amount", out var amount) && amount.TryGetInt64(out var parsedAmount)
                ? parsedAmount
                : -1;
            currency = GetString(entity, "currency");
            paymentStatus = GetString(entity, "status");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Rejected malformed Razorpay webhook payload.");
            return BadRequest("Invalid Razorpay webhook payload.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
            return BadRequest("Missing Razorpay event type.");

        if (eventType is not ("payment.captured" or "payment.failed"))
            return Ok(new { received = true, ignored = true });

        if (string.IsNullOrWhiteSpace(razorpayPaymentId) || string.IsNullOrWhiteSpace(razorpayOrderId))
            return BadRequest("Payment or order reference is missing.");

        Order? paidOrderForFollowUp = null;
        PaymentDeliveryAddress? deliveryAddress = null;
        var shouldSendConfirmation = false;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var webhookEvent = new RazorpayWebhookEvent
        {
            EventId = eventId,
            EventType = eventType,
            RazorpayPaymentId = razorpayPaymentId,
            RazorpayOrderId = razorpayOrderId,
            ProcessingStatus = "Received"
        };
        db.RazorpayWebhookEvents.Add(webhookEvent);

        try
        {
            // The unique EventId index makes repeated Razorpay deliveries safe.
            await db.SaveChangesAsync(cancellationToken);

            var payment = await db.Payments
                .FromSqlInterpolated($"SELECT * FROM dbo.Payments WITH (UPDLOCK, ROWLOCK) WHERE RazorpayOrderId = {razorpayOrderId}")
                .Include(x => x.Order).ThenInclude(x => x.Items)
                .Include(x => x.Order).ThenInclude(x => x.Customer)
                .Include(x => x.Order).ThenInclude(x => x.DelhiveryShipment)
                .SingleOrDefaultAsync(cancellationToken);

            if (payment is null || payment.Order is null)
            {
                webhookEvent.ProcessingStatus = "Ignored";
                webhookEvent.ErrorMessage = "No matching local payment/order was found.";
            }
            else if (eventType == "payment.captured")
            {
                var order = payment.Order;
                var expectedAmount = checked((long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero));

                if (!string.Equals(paymentStatus, "captured", StringComparison.OrdinalIgnoreCase) ||
                    amountInPaise != expectedAmount ||
                    !string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(payment.RazorpayOrderId, razorpayOrderId, StringComparison.Ordinal))
                {
                    webhookEvent.ProcessingStatus = "Rejected";
                    webhookEvent.ErrorMessage = "Captured payment status, amount, currency or order reference did not match the local order.";
                    logger.LogError("Razorpay captured event did not match local order {OrderId}.", order.Id);
                }
                else if (order.PaymentStatus == "Paid" || payment.Status == "Captured")
                {
                    webhookEvent.ProcessingStatus = "Processed";
                }
                else
                {
                    var productIds = order.Items.Select(x => x.ProductId).Distinct().ToList();
                    var products = await db.Products
                        .Include(x => x.Variants)
                        .Where(x => productIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var unavailableItem = order.Items.FirstOrDefault(item =>
                    {
                        var product = products.SingleOrDefault(x => x.Id == item.ProductId);
                        var variant = product?.Variants.FirstOrDefault(x =>
                            x.Size == item.Size &&
                            (string.IsNullOrWhiteSpace(item.Color) ||
                             string.Equals(x.Color, item.Color, StringComparison.OrdinalIgnoreCase)));
                        return variant is null || variant.Stock < item.Quantity;
                    });

                    if (unavailableItem is not null)
                    {
                        webhookEvent.ProcessingStatus = "NeedsReview";
                        webhookEvent.ErrorMessage = $"Payment was captured, but stock is unavailable for {unavailableItem.ProductName} ({unavailableItem.Size}). Manual refund/order review is required.";
                        logger.LogCritical("Captured Razorpay payment for order {OrderId}, but inventory is unavailable for {ProductName} ({Size}).",
                            order.Id, unavailableItem.ProductName, unavailableItem.Size);
                    }
                    else
                    {
                        foreach (var item in order.Items)
                        {
                            var product = products.Single(x => x.Id == item.ProductId);
                            var variant = product.Variants.First(x =>
                                x.Size == item.Size &&
                                (string.IsNullOrWhiteSpace(item.Color) ||
                                 string.Equals(x.Color, item.Color, StringComparison.OrdinalIgnoreCase)));
                            variant.Stock -= item.Quantity;
                            db.InventoryTransactions.Add(new InventoryTransaction
                            {
                                ProductId = product.Id,
                                Size = variant.Size,
                                Type = "Sale",
                                Quantity = -item.Quantity,
                                StockAfter = variant.Stock,
                                Note = $"Razorpay webhook payment for order #{order.Id}"
                            });
                        }

                        order.PaymentStatus = "Paid";
                        order.Status = "Processing";
                        payment.Status = "Captured";
                        payment.RazorpayPaymentId = razorpayPaymentId;
                        payment.PaidDate = DateTime.UtcNow;
                        webhookEvent.ProcessingStatus = "Processed";
                        paidOrderForFollowUp = order;
                        shouldSendConfirmation = true;

                        if (!string.IsNullOrWhiteSpace(order.DeliveryAddressJson))
                        {
                            try
                            {
                                deliveryAddress = JsonSerializer.Deserialize<PaymentDeliveryAddress>(order.DeliveryAddressJson);
                            }
                            catch (JsonException ex)
                            {
                                logger.LogError(ex, "Saved delivery address could not be read for order {OrderId}.", order.Id);
                            }
                        }
                    }
                }
            }
            else
            {
                var order = payment?.Order;
                if (payment is not null && order is not null && order.PaymentStatus != "Paid" && payment.Status != "Captured")
                {
                    payment.Status = "Failed";
                    order.PaymentStatus = "Failed";
                }

                webhookEvent.ProcessingStatus = "Processed";
            }

            webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            if (await db.RazorpayWebhookEvents.AnyAsync(x => x.EventId == eventId, cancellationToken))
            {
                logger.LogInformation("Ignored duplicate Razorpay webhook event {EventId}.", eventId);
                return Ok(new { received = true, duplicate = true });
            }

            logger.LogError(ex, "Unable to persist Razorpay webhook event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to process Razorpay webhook event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        if (paidOrderForFollowUp is not null)
        {
            if (deliveryAddress is not null)
            {
                try
                {
                    await PaymentsController.CreatePaidOrderShipmentAsync(
                        paidOrderForFollowUp, deliveryAddress, delhivery, db, configuration, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Order {OrderId} is paid, but webhook shipment creation failed.", paidOrderForFollowUp.Id);
                }
            }
            else
            {
                logger.LogWarning("Order {OrderId} is paid by webhook, but no structured delivery address was saved; shipment creation was skipped.", paidOrderForFollowUp.Id);
            }

            if (shouldSendConfirmation)
            {
                try
                {
                    await orderEmail.SendOrderConfirmationAsync(paidOrderForFollowUp, paidOrderForFollowUp.Customer);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Order {OrderId} is paid, but confirmation email could not be sent.", paidOrderForFollowUp.Id);
                }
            }
        }

        return Ok(new { received = true });
    }

    private static bool TryReadSignature(string signature, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(signature) || signature.Length != 64)
            return false;

        try
        {
            bytes = Convert.FromHexString(signature);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
