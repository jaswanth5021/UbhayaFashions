using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class CheckoutSafety
{
    public static bool TestPaymentsAllowed(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("PaymentTesting:Enabled");

    public static bool IsCancelled(Order order) =>
        string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

    public static string? ValidateAddress(PaymentDeliveryAddress? address)
    {
        if (address is null || string.IsNullOrWhiteSpace(address.Name) || address.Name.Length > 150 ||
            string.IsNullOrWhiteSpace(address.AddressLine1) ||
            address.AddressLine1.Length + (address.AddressLine2?.Length ?? 0) > 490 ||
            string.IsNullOrWhiteSpace(address.City) || address.City.Length > 100 ||
            string.IsNullOrWhiteSpace(address.State) || address.State.Length > 100)
            return "Enter a complete delivery address within the allowed length.";
        if (address.Mobile is null || !System.Text.RegularExpressions.Regex.IsMatch(address.Mobile, @"^\+?[0-9]{10,15}$"))
            return "Enter a valid delivery phone number.";
        if (address.PostalCode is null || !System.Text.RegularExpressions.Regex.IsMatch(address.PostalCode, "^[1-9][0-9]{5}$"))
            return "Enter a valid six-digit delivery PIN code.";
        if (!string.Equals(address.Country, "India", StringComparison.OrdinalIgnoreCase))
            return "Delivery is currently available within India only.";
        return null;
    }

    public static string Normalize(string? value) => (value ?? "").Trim().ToUpperInvariant();

    // Validate the combined demand before mutating any tracked entity.
    public static List<(ProductVariant Variant, int Quantity)> PlanStock(
        IEnumerable<OrderItem> items, IReadOnlyCollection<ProductVariant> variants)
    {
        var result = new List<(ProductVariant, int)>();
        foreach (var group in items.GroupBy(x => (x.ProductId, Size: Normalize(x.Size), Color: Normalize(x.Color))))
        {
            if (group.Any(x => x.Quantity <= 0)) throw new InvalidOperationException("Invalid item quantity.");
            var quantity = checked(group.Sum(x => x.Quantity));
            var productVariants = variants.Where(x => x.ProductId == group.Key.ProductId).ToList();
            var matches = productVariants.Where(x => Normalize(x.Size) == group.Key.Size &&
                (Normalize(x.Color) == group.Key.Color ||
                 (group.Key.Color.Length == 0 && productVariants.Count(v => Normalize(v.Size) == group.Key.Size) == 1)))
                .ToList();
            if (matches.Count != 1) throw new InvalidOperationException("The ordered size/colour is unavailable or ambiguous.");
            result.Add((matches[0], quantity));
        }
        // Legacy no-colour and explicit-colour lines can resolve to the same row.
        return result.GroupBy(x => x.Item1.Id)
            .Select(g => (g.First().Item1, checked(g.Sum(x => x.Item2)))).ToList();
    }

    public static Task<Order?> LockOrderAsync(ApplicationDbContext db, int id, CancellationToken ct = default) =>
        db.Orders.FromSqlInterpolated($"SELECT * FROM dbo.Orders WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .Include(x => x.Items).Include(x => x.Payments).Include(x => x.Customer)
            .Include(x => x.DelhiveryShipment).SingleOrDefaultAsync(ct);

    public static async Task<List<ProductVariant>> LockVariantsAsync(ApplicationDbContext db,
        IEnumerable<int> productIds, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Stock changes require a transaction.");
        var variants = new List<ProductVariant>();
        // Consistent lock ordering avoids opposite-cart-order deadlocks.
        foreach (var id in productIds.Distinct().Order())
            variants.AddRange(await db.ProductVariants
                .FromSqlInterpolated($"SELECT * FROM dbo.ProductVariants WITH (UPDLOCK, HOLDLOCK) WHERE ProductId = {id}")
                .ToListAsync(ct));
        return variants;
    }

    public static void ApplyStock(ApplicationDbContext db, Order order,
        IEnumerable<(ProductVariant Variant, int Quantity)> plan, bool restore)
    {
        foreach (var (variant, quantity) in plan)
        {
            variant.Stock = checked(variant.Stock + (restore ? quantity : -quantity));
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = variant.ProductId, Size = variant.Size,
                Type = restore ? "Cancellation" : "Sale", Quantity = restore ? quantity : -quantity,
                StockAfter = variant.Stock,
                Note = $"Order #{order.Id}; colour {variant.Color}"
            });
        }
        order.InventoryCommitted = !restore;
    }

    // Caller holds the order lock and commits payment, inventory and follow-up state together.
    public static async Task ApplyCaptureAsync(ApplicationDbContext db, Order order, Payment payment,
        string providerPaymentId, CancellationToken ct = default)
    {
        if (payment.PaidDate.HasValue) return; // Includes captured, refund-pending and refunded payments.
        payment.Status = "Captured";
        payment.RazorpayPaymentId = providerPaymentId;
        payment.PaidDate = DateTime.UtcNow;
        if (IsCancelled(order))
        {
            order.PaymentStatus = "Refund Pending";
            return;
        }
        var variants = await LockVariantsAsync(db, order.Items.Select(x => x.ProductId), ct);
        List<(ProductVariant Variant, int Quantity)> plan;
        try { plan = PlanStock(order.Items, variants); }
        catch (InvalidOperationException) { MarkReview(order); return; }
        catch (OverflowException) { MarkReview(order); return; }
        if (plan.Any(x => x.Variant.Stock < x.Quantity)) { MarkReview(order); return; }
        if (!order.InventoryCommitted) ApplyStock(db, order, plan, false);
        order.PaymentStatus = "Paid";
        order.Status = "Processing";
        order.FollowUpStatus = "Pending";
    }

    private static void MarkReview(Order order)
    {
        order.PaymentStatus = "Refund Pending";
        order.Status = "Payment Review";
        order.FollowUpStatus = "Review";
    }
}
