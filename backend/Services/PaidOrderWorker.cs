using System.Text.Json;
using backend.Controllers;
using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

// The order's Pending flag is written in the payment transaction. Work survives process restarts.
// An interrupted courier request is parked for reconciliation, never blindly submitted twice.
public sealed class PaidOrderWorker(IServiceScopeFactory scopes, ILogger<PaidOrderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessNextAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Paid-order follow-up worker failed; will retry polling."); }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // A previous process may have stopped after the courier accepted a request.
        var staleBefore = DateTime.UtcNow.AddMinutes(-15);
        await db.Orders.Where(x => x.FollowUpStatus == "InProgress" && x.FollowUpAttemptAtUtc < staleBefore)
            .ExecuteUpdateAsync(x => x.SetProperty(o => o.FollowUpStatus, "Review"), ct);
        var retryBefore = DateTime.UtcNow.AddMinutes(-5);
        var id = await db.Orders.AsNoTracking()
            .Where(x => x.FollowUpStatus == "Pending" ||
                (x.FollowUpStatus == "EmailRetry" && x.FollowUpAttemptAtUtc < retryBefore))
            .OrderBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
        if (!id.HasValue) return;
        backend.Models.Order? order;
        bool emailOnly;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            order = await CheckoutSafety.LockOrderAsync(db, id.Value, ct);
            if (order is null || (order.FollowUpStatus != "Pending" && order.FollowUpStatus != "EmailRetry")) return;
            if (order.PaymentStatus != "Paid" || CheckoutSafety.IsCancelled(order))
            {
                order.FollowUpStatus = "Cancelled";
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }
            emailOnly = order.FollowUpStatus == "EmailRetry";
            order.FollowUpStatus = "InProgress";
            order.FollowUpAttemptAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        if (!emailOnly)
        {
            try
            {
                var address = JsonSerializer.Deserialize<PaymentDeliveryAddress>(order.DeliveryAddressJson ?? "null");
                if (CheckoutSafety.ValidateAddress(address) is not null) throw new InvalidOperationException("Saved delivery address requires review.");
                await PaymentsController.CreatePaidOrderShipmentAsync(order, address,
                    scope.ServiceProvider.GetRequiredService<IDelhiveryService>(), db,
                    scope.ServiceProvider.GetRequiredService<IConfiguration>(), logger);
                if (string.IsNullOrWhiteSpace(order.DelhiveryShipment?.Waybill))
                    throw new InvalidOperationException("Courier creation requires reconciliation before retry.");
            }
            catch (Exception ex)
            {
                order.FollowUpStatus = "Review";
                await db.SaveChangesAsync(ct);
                logger.LogError(ex, "Paid order {OrderId} needs courier review; reconcile UBHAYA-{OrderId} before resubmission.", order.Id, order.Id);
                return;
            }
        }
        try
        {
            await scope.ServiceProvider.GetRequiredService<IOrderConfirmationEmailService>()
                .SendOrderConfirmationAsync(order, order.Customer);
            order.FollowUpStatus = "Complete";
        }
        catch (Exception ex)
        {
            order.FollowUpStatus = "EmailRetry";
            logger.LogWarning(ex, "Confirmation email for order {OrderId} will be retried.", order.Id);
        }
        await db.SaveChangesAsync(ct);
        var payment = order.Payments.FirstOrDefault(x => x.Status == "Captured");
        if (payment is not null)
            await scope.ServiceProvider.GetRequiredService<IGa4MeasurementProtocolService>().TrackPurchaseAsync(payment.Id, ct);
    }
}
