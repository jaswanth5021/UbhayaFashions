using backend.Models;
using backend.Services;
using Xunit;

namespace UbhayaFashions.Tests;

public class CouponEligibilityTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ProductCoupon_AppliesWhenEveryCartItemMatches()
    {
        var result = Evaluate(new Coupon { ProductId = 1014, DiscountType = "Percentage", DiscountValue = 10m },
            new CouponCartLine(1014, 8, "Office Wear Linen Kurti", 999m));

        Assert.True(result.IsEligible);
        Assert.Equal(999m, result.EligibleSubtotal);
        Assert.Equal(99.90m, result.DiscountAmount);
    }

    [Fact]
    public void ProductCoupon_RejectsMixedProductCartAndDoesNotDiscountMatchingLines()
    {
        var result = Evaluate(new Coupon { ProductId = 1014, DiscountType = "Percentage", DiscountValue = 10m },
            new CouponCartLine(1014, 8, "Office Wear Linen Kurti", 999m),
            new CouponCartLine(1056, 9, "Floral Summer Dress", 1199m));

        Assert.False(result.IsEligible);
        Assert.Equal(999m, result.EligibleSubtotal);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("Office Wear Linen Kurti", result.RejectionReason);
    }

    [Fact]
    public void CategoryCoupon_AppliesWhenEveryCartItemHasTheTargetCategory()
    {
        var result = Evaluate(new Coupon { CategoryId = 8, DiscountType = "Percentage", DiscountValue = 10m },
            new CouponCartLine(1014, 8, "Kurti A", 999m),
            new CouponCartLine(1015, 8, "Kurti B", 500m));

        Assert.True(result.IsEligible);
        Assert.Equal(149.90m, result.DiscountAmount);
    }

    [Fact]
    public void CategoryCoupon_RejectsCartWithAnotherCategory()
    {
        var result = Evaluate(new Coupon { CategoryId = 8, DiscountType = "Percentage", DiscountValue = 10m },
            new CouponCartLine(1014, 8, "Kurti", 999m),
            new CouponCartLine(1056, 9, "Dress", 1199m));

        Assert.False(result.IsEligible);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("eligible category", result.RejectionReason);
    }

    [Fact]
    public void CombinedProductAndCategoryScope_RequiresBothToMatch()
    {
        var matching = Evaluate(new Coupon { ProductId = 1014, CategoryId = 8, DiscountType = "Fixed", DiscountValue = 50m },
            new CouponCartLine(1014, 8, "Kurti", 999m));
        var wrongCategory = Evaluate(new Coupon { ProductId = 1014, CategoryId = 8, DiscountType = "Fixed", DiscountValue = 50m },
            new CouponCartLine(1014, 9, "Kurti", 999m));

        Assert.True(matching.IsEligible);
        Assert.Equal(50m, matching.DiscountAmount);
        Assert.False(wrongCategory.IsEligible);
        Assert.Equal(0m, wrongCategory.DiscountAmount);
    }

    [Fact]
    public void UnrestrictedCoupon_AppliesAcrossDifferentProductsAndCategories()
    {
        var result = Evaluate(new Coupon { DiscountType = "Percentage", DiscountValue = 10m },
            new CouponCartLine(1014, 8, "Kurti", 999m),
            new CouponCartLine(1056, 9, "Dress", 1199m));

        Assert.True(result.IsEligible);
        Assert.Equal(219.80m, result.DiscountAmount);
    }

    [Fact]
    public void ProductCoupon_RejectsCartContainingOnlyAnotherProduct()
    {
        var result = Evaluate(new Coupon { ProductId = 1014, DiscountType = "Fixed", DiscountValue = 100m },
            new CouponCartLine(1056, 9, "Floral Summer Dress", 1199m));

        Assert.False(result.IsEligible);
        Assert.Equal(0m, result.DiscountAmount);
    }

    [Fact]
    public void ExpiredCoupon_IsRejected()
    {
        var result = Evaluate(new Coupon
        {
            DiscountType = "Fixed",
            DiscountValue = 100m,
            EndsAtUtc = Now.AddSeconds(-1)
        }, new CouponCartLine(1014, 8, "Kurti", 999m));

        Assert.False(result.IsEligible);
        Assert.Contains("expired", result.RejectionReason);
    }

    [Fact]
    public void PercentageDiscount_RespectsMaximumDiscountAndMinimumOrder()
    {
        var capped = Evaluate(new Coupon
        {
            DiscountType = "Percentage",
            DiscountValue = 10m,
            MaximumDiscount = 100m,
            MinimumOrderAmount = 1000m
        }, new CouponCartLine(1014, 8, "Kurti", 2198m));
        var belowMinimum = Evaluate(new Coupon
        {
            DiscountType = "Percentage",
            DiscountValue = 10m,
            MinimumOrderAmount = 1000m
        }, new CouponCartLine(1014, 8, "Kurti", 999m));

        Assert.True(capped.IsEligible);
        Assert.Equal(100m, capped.DiscountAmount);
        Assert.False(belowMinimum.IsEligible);
    }

    [Fact]
    public void FirstOrderAndPerCustomerLimits_AreEnforced()
    {
        var firstOrderCoupon = new Coupon { DiscountType = "Fixed", DiscountValue = 100m, FirstOrderOnly = true };
        var limitedCoupon = new Coupon { DiscountType = "Fixed", DiscountValue = 100m, PerCustomerLimit = 1 };
        var cart = new[] { new CouponCartLine(1014, 8, "Kurti", 999m) };

        Assert.False(CouponEligibility.Evaluate(firstOrderCoupon, cart, Now, hasPreviousOrder: true, customerRedemptionCount: 0).IsEligible);
        Assert.False(CouponEligibility.Evaluate(limitedCoupon, cart, Now, hasPreviousOrder: false, customerRedemptionCount: 1).IsEligible);
    }

    private static CouponEvaluation Evaluate(Coupon coupon, params CouponCartLine[] cart)
        => CouponEligibility.Evaluate(coupon, cart, Now, hasPreviousOrder: false, customerRedemptionCount: 0);
}
