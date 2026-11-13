using backend.Models;

namespace backend.Services;

public sealed record CouponCartLine(int ProductId, int CategoryId, string ProductName, decimal Amount);

public sealed record CouponEvaluation(
    bool IsEligible,
    decimal EligibleSubtotal,
    decimal DiscountAmount,
    string RejectionReason);

public static class CouponEligibility
{
    public static CouponEvaluation Evaluate(
        Coupon coupon,
        IReadOnlyCollection<CouponCartLine> cart,
        DateTime nowUtc,
        bool hasPreviousOrder,
        int customerRedemptionCount)
    {
        var matchingLines = cart.Where(line =>
            (!coupon.ProductId.HasValue || line.ProductId == coupon.ProductId.Value) &&
            (!coupon.CategoryId.HasValue || line.CategoryId == coupon.CategoryId.Value)).ToList();
        var eligibleSubtotal = matchingLines.Sum(line => line.Amount);
        var productScopeMatches = !coupon.ProductId.HasValue || cart.All(line => line.ProductId == coupon.ProductId.Value);
        var categoryScopeMatches = !coupon.CategoryId.HasValue || cart.All(line => line.CategoryId == coupon.CategoryId.Value);

        if (!coupon.IsActive)
            return Reject(eligibleSubtotal, "This coupon is inactive.");
        if (coupon.StartsAtUtc.HasValue && coupon.StartsAtUtc.Value > nowUtc)
            return Reject(eligibleSubtotal, "This coupon is not active yet.");
        if (coupon.EndsAtUtc.HasValue && coupon.EndsAtUtc.Value < nowUtc)
            return Reject(eligibleSubtotal, "This coupon has expired.");
        if (coupon.MaximumUses.HasValue && coupon.UsageCount >= coupon.MaximumUses.Value)
            return Reject(eligibleSubtotal, "This coupon has reached its usage limit.");
        if (coupon.DiscountType is not ("Percentage" or "Fixed") || coupon.DiscountValue <= 0m ||
            (coupon.DiscountType == "Percentage" && coupon.DiscountValue > 100m) ||
            (coupon.MaximumDiscount.HasValue && coupon.MaximumDiscount.Value < 0m) || coupon.MinimumOrderAmount < 0m)
            return Reject(eligibleSubtotal, "This coupon has invalid discount settings.");
        if (cart.Count == 0)
            return Reject(eligibleSubtotal, "A valid cart is required.");

        if (!productScopeMatches)
        {
            var productName = cart.FirstOrDefault(line => line.ProductId == coupon.ProductId)?.ProductName;
            return Reject(eligibleSubtotal,
                $"This coupon applies only to {productName ?? "the eligible product"}. Remove other products from your cart to use this coupon.");
        }
        if (!categoryScopeMatches)
            return Reject(eligibleSubtotal,
                "This coupon applies only to products in the eligible category. Remove products from other categories to use this coupon.");
        if (eligibleSubtotal < coupon.MinimumOrderAmount)
            return Reject(eligibleSubtotal,
                $"This coupon requires a minimum order value of ₹{coupon.MinimumOrderAmount:0.##}.");
        if (coupon.FirstOrderOnly && hasPreviousOrder)
            return Reject(eligibleSubtotal, "This offer is for your first order only.");
        if (coupon.PerCustomerLimit.HasValue && customerRedemptionCount >= coupon.PerCustomerLimit.Value)
            return Reject(eligibleSubtotal, "You have already used this offer.");

        var discount = coupon.DiscountType == "Percentage"
            ? eligibleSubtotal * coupon.DiscountValue / 100m
            : coupon.DiscountValue;
        if (coupon.MaximumDiscount.HasValue)
            discount = Math.Min(discount, coupon.MaximumDiscount.Value);
        discount = Math.Round(Math.Clamp(discount, 0m, eligibleSubtotal), 2, MidpointRounding.AwayFromZero);
        if (discount <= 0m)
            return Reject(eligibleSubtotal, "This offer does not add a discount to your current cart.");

        return new CouponEvaluation(true, eligibleSubtotal, discount, string.Empty);
    }

    private static CouponEvaluation Reject(decimal eligibleSubtotal, string reason)
        => new(false, eligibleSubtotal, 0m, reason);
}
