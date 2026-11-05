using System.Net.Http.Json;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public interface IGa4MeasurementProtocolService
{
    Task<bool> TrackPurchaseAsync(int paymentId, CancellationToken cancellationToken = default);
}

public sealed class Ga4MeasurementProtocolService(
    ApplicationDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Ga4MeasurementProtocolService> logger) : IGa4MeasurementProtocolService
{
    public async Task<bool> TrackPurchaseAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var measurementId = configuration["GA4:MeasurementId"];
        var apiSecret = configuration["GA4:ApiSecret"];

        if (string.IsNullOrWhiteSpace(measurementId) || string.IsNullOrWhiteSpace(apiSecret))
        {
            logger.LogWarning("GA4 purchase tracking skipped: GA4:MeasurementId or GA4:ApiSecret is not configured.");
            return false;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Serialize verification and webhook attempts for this payment.
            var payment = await db.Payments
                .FromSqlInterpolated($"SELECT * FROM dbo.Payments WITH (UPDLOCK, ROWLOCK) WHERE Id = {paymentId}")
                .SingleOrDefaultAsync(cancellationToken);

            if (payment is null || payment.Ga4PurchaseSent ||
                !string.Equals(payment.Status, "Captured", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var order = await db.Orders
                .Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.Id == payment.OrderId, cancellationToken);

            if (order is null || !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var payload = new
            {
                // Webhook purchases can arrive after the shopper closes the browser.
                client_id = $"ubhaya.order.{order.Id}",
                events = new[]
                {
                    new
                    {
                        name = "purchase",
                        @params = new
                        {
                            transaction_id = order.Id.ToString(),
                            value = order.TotalAmount,
                            currency = "INR",
                            items = order.Items.Select(item => new
                            {
                                item_id = item.ProductId.ToString(),
                                item_name = item.ProductName,
                                item_variant = string.Join(" / ", new[] { item.Size, item.Color }
                                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                                price = item.Price,
                                quantity = item.Quantity
                            }).ToArray()
                        }
                    }
                }
            };

            var url = "https://www.google-analytics.com/mp/collect" +
                $"?measurement_id={Uri.EscapeDataString(measurementId)}" +
                $"&api_secret={Uri.EscapeDataString(apiSecret)}";

            var client = httpClientFactory.CreateClient("GA4");
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GA4 purchase event for order {OrderId} returned HTTP {StatusCode}.",
                    order.Id, (int)response.StatusCode);
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            payment.Ga4PurchaseSent = true;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("GA4 purchase event sent for order {OrderId}.", order.Id);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "GA4 purchase tracking failed for payment {PaymentId}.", paymentId);
            return false;
        }
    }
}
