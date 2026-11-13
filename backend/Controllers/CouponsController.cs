using System.Security.Claims;
using backend.Data;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/coupons")]
[Authorize(Roles = "Customer")]
public class CouponsController(ApplicationDbContext db, ILogger<CouponsController> logger) : ControllerBase
{
    [HttpPost("checkout-offers")]
    public async Task<IActionResult> CheckoutOffers(CheckoutCouponOffersRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();
        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 100 ||
            request.Items.Any(item => item.Quantity <= 0 || item.Quantity > 100 || string.IsNullOrWhiteSpace(item.Size)))
            return BadRequest("A valid cart is required.");

        var cartResult = await LoadCartAsync(request.Items);
        if (cartResult.Error is not null) return BadRequest(cartResult.Error);
        var cartLines = cartResult.Lines;
        var now = DateTime.UtcNow;
        var coupons = await db.Coupons.AsNoTracking()
            .Where(coupon => coupon.IsActive &&
                (!coupon.StartsAtUtc.HasValue || coupon.StartsAtUtc <= now) &&
                (!coupon.EndsAtUtc.HasValue || coupon.EndsAtUtc >= now) &&
                (!coupon.MaximumUses.HasValue || coupon.UsageCount < coupon.MaximumUses))
            .ToListAsync();
        var hasPreviousOrder = await CouponCustomerHistory.HasPreviousOrderAsync(db, customerId);
        var redeemedCouponIds = await db.CouponRedemptions
            .Where(redemption => redemption.CustomerId == customerId)
            .Select(redemption => redemption.CouponId)
            .ToListAsync();

        var offers = coupons.Select(coupon =>
        {
            var evaluation = CouponEligibility.Evaluate(coupon, cartLines, now,
                hasPreviousOrder, redeemedCouponIds.Count(id => id == coupon.Id));
            LogEvaluation(coupon.Code, coupon.ProductId, coupon.CategoryId, cartLines, evaluation);
            return new { Coupon = coupon, Evaluation = evaluation };
        })
        .Where(offer => offer.Evaluation.EligibleSubtotal > 0m)
        .OrderByDescending(offer => offer.Evaluation.DiscountAmount)
        .ThenBy(offer => offer.Coupon.Name)
        .Take(8)
        .Select(offer => new
        {
            offer.Coupon.Id,
            code = offer.Coupon.IsAutomatic ? null : offer.Coupon.Code,
            offer.Coupon.Name,
            discountText = offer.Coupon.DiscountType == "Percentage"
                ? $"{offer.Coupon.DiscountValue:0.##}% OFF" + (offer.Coupon.MaximumDiscount.HasValue ? $" up to ₹{offer.Coupon.MaximumDiscount.Value:0.##}" : "")
                : $"₹{offer.Coupon.DiscountValue:0.##} OFF",
            discountAmount = offer.Evaluation.DiscountAmount,
            canApply = offer.Evaluation.IsEligible,
            eligibilityMessage = offer.Evaluation.RejectionReason,
            discountType = offer.Coupon.DiscountType,
            discountValue = offer.Coupon.DiscountValue,
            maximumDiscount = offer.Coupon.MaximumDiscount,
            discountDataAvailable = true,
            productId = offer.Coupon.ProductId,
            categoryId = offer.Coupon.CategoryId,
            scopeDataAvailable = true,
            offer.Coupon.MinimumOrderAmount,
            offer.Coupon.IsAutomatic,
            offer.Coupon.FirstOrderOnly,
            appliesTo = offer.Coupon.ProductId.HasValue && offer.Coupon.CategoryId.HasValue ? "Selected product and category" :
                        offer.Coupon.ProductId.HasValue ? "Selected product" :
                        offer.Coupon.CategoryId.HasValue ? "Selected category" : "All products",
            offer.Coupon.EndsAtUtc,
            terms = string.Join(" · ", new[]
            {
                offer.Coupon.MinimumOrderAmount > 0 ? $"Minimum order ₹{offer.Coupon.MinimumOrderAmount:0.##}" : null,
                offer.Coupon.MaximumDiscount.HasValue && offer.Coupon.DiscountType == "Percentage" ? $"Maximum discount ₹{offer.Coupon.MaximumDiscount.Value:0.##}" : null,
                offer.Coupon.FirstOrderOnly ? "First order only" : null,
                offer.Coupon.IsAutomatic ? "Applied automatically at checkout" : "Enter the code to apply"
            }.Where(value => value is not null))
        })
        .ToList();

        return Ok(offers);
    }

    [AllowAnonymous]
    [HttpGet("product/{productId:int}/offers")]
    public async Task<IActionResult> ProductOffers(int productId)
    {
        var product = await db.Products.AsNoTracking()
            .Where(item => item.Id == productId)
            .Select(item => new { item.Id, item.CategoryId })
            .FirstOrDefaultAsync();
        if (product is null) return NotFound();

        var now = DateTime.UtcNow;
        var offers = await db.Coupons.AsNoTracking()
            .Where(coupon => coupon.IsActive)
            .Where(coupon => (!coupon.StartsAtUtc.HasValue || coupon.StartsAtUtc <= now) &&
                             (!coupon.EndsAtUtc.HasValue || coupon.EndsAtUtc >= now))
            .Where(coupon => !coupon.MaximumUses.HasValue || coupon.UsageCount < coupon.MaximumUses)
            .Where(coupon => (!coupon.ProductId.HasValue || coupon.ProductId == product.Id) &&
                             (!coupon.CategoryId.HasValue || coupon.CategoryId == product.CategoryId))
            .OrderByDescending(coupon => coupon.DiscountType == "Percentage" ? coupon.DiscountValue : 0)
            .ThenBy(coupon => coupon.MinimumOrderAmount)
            .Select(coupon => new
            {
                coupon.Id,
                coupon.Code,
                coupon.Name,
                coupon.DiscountType,
                coupon.DiscountValue,
                coupon.MaximumDiscount,
                coupon.MinimumOrderAmount,
                coupon.IsAutomatic,
                coupon.FirstOrderOnly,
                coupon.ProductId,
                coupon.CategoryId,
                coupon.EndsAtUtc
            })
            .Take(8)
            .ToListAsync();

        return Ok(offers.Select(offer => new
        {
            offer.Id,
            code = offer.IsAutomatic ? null : offer.Code,
            offer.Name,
            discountText = offer.DiscountType == "Percentage"
                ? $"{offer.DiscountValue:0.##}% OFF" + (offer.MaximumDiscount.HasValue ? $" up to ₹{offer.MaximumDiscount.Value:0.##}" : "")
                : $"₹{offer.DiscountValue:0.##} OFF",
            offer.MinimumOrderAmount,
            discountType = offer.DiscountType,
            discountValue = offer.DiscountValue,
            maximumDiscount = offer.MaximumDiscount,
            discountDataAvailable = true,
            productId = offer.ProductId,
            categoryId = offer.CategoryId,
            scopeDataAvailable = true,
            offer.IsAutomatic,
            offer.FirstOrderOnly,
            appliesTo = offer.ProductId.HasValue && offer.CategoryId.HasValue ? "Selected product and category" :
                        offer.ProductId.HasValue ? "This product" :
                        offer.CategoryId.HasValue ? "Selected category" : "All products",
            offer.EndsAtUtc,
            terms = string.Join(" · ", new[]
            {
                offer.MinimumOrderAmount > 0 ? $"Minimum order ₹{offer.MinimumOrderAmount:0.##}" : null,
                offer.MaximumDiscount.HasValue && offer.DiscountType == "Percentage" ? $"Maximum discount ₹{offer.MaximumDiscount.Value:0.##}" : null,
                offer.FirstOrderOnly ? "First order only" : null,
                offer.IsAutomatic ? "Applied automatically at checkout" : "Enter the code at checkout"
            }.Where(value => value is not null))
        }));
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(CouponApplyRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 100 ||
            request.Items.Any(item => item.Quantity <= 0 || item.Quantity > 100 || string.IsNullOrWhiteSpace(item.Size)))
            return BadRequest("A valid cart is required.");

        var cartResult = await LoadCartAsync(request.Items);
        if (cartResult.Error is not null) return BadRequest(cartResult.Error);
        var cartLines = cartResult.Lines;
        var now = DateTime.UtcNow;
        var normalizedCode = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var coupons = string.IsNullOrWhiteSpace(normalizedCode)
            ? await db.Coupons.AsNoTracking().Where(coupon => coupon.IsAutomatic).ToListAsync()
            : await db.Coupons.AsNoTracking().Where(coupon => coupon.Code == normalizedCode).ToListAsync();
        var hasPreviousOrder = await CouponCustomerHistory.HasPreviousOrderAsync(db, customerId);
        var redemptionCounts = await db.CouponRedemptions
            .Where(redemption => redemption.CustomerId == customerId)
            .GroupBy(redemption => redemption.CouponId)
            .Select(group => new { CouponId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CouponId, item => item.Count);

        var evaluated = coupons.Select(coupon =>
        {
            var evaluation = CouponEligibility.Evaluate(coupon, cartLines, now, hasPreviousOrder,
                redemptionCounts.GetValueOrDefault(coupon.Id));
            LogEvaluation(coupon.Code, coupon.ProductId, coupon.CategoryId, cartLines, evaluation);
            return new { Coupon = coupon, Evaluation = evaluation };
        }).ToList();
        var eligible = evaluated.Where(item => item.Evaluation.IsEligible)
            .OrderByDescending(item => item.Evaluation.DiscountAmount).ToList();

        if (eligible.Count == 0)
        {
            var message = evaluated.FirstOrDefault()?.Evaluation.RejectionReason ??
                "This coupon is invalid or does not apply to your cart.";
            return BadRequest(new { message });
        }

        var chosen = eligible[0];
        var subtotal = cartLines.Sum(line => line.Amount);
        return Ok(new
        {
            code = chosen.Coupon.IsAutomatic ? (string?)null : chosen.Coupon.Code,
            name = chosen.Coupon.Name,
            discountAmount = chosen.Evaluation.DiscountAmount,
            subtotal,
            payableSubtotal = Math.Max(0m, subtotal - chosen.Evaluation.DiscountAmount),
            isAutomatic = chosen.Coupon.IsAutomatic
        });
    }

    private async Task<(List<CouponCartLine> Lines, string? Error)> LoadCartAsync(List<CreateOrderItem> items)
    {
        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Include(product => product.Variants)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();
        if (products.Count != productIds.Count)
            return ([], "One or more products in your cart are no longer available.");

        var lines = new List<CouponCartLine>();
        foreach (var item in items)
        {
            var product = products.Single(product => product.Id == item.ProductId);
            var hasColorVariants = product.Variants.Any(variant => !string.IsNullOrWhiteSpace(variant.Color));
            var sizeVariants = product.Variants
                .Where(variant => string.Equals(variant.Size, item.Size, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var variant = hasColorVariants
                ? sizeVariants.FirstOrDefault(candidate =>
                    !string.IsNullOrWhiteSpace(item.Color) &&
                    string.Equals(candidate.Color, item.Color, StringComparison.OrdinalIgnoreCase))
                    ?? sizeVariants.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.Color))
                : sizeVariants.FirstOrDefault();
            if (variant is null || variant.Stock < item.Quantity)
                return ([], $"{product.Name} ({item.Size}) is no longer available in that quantity.");
            lines.Add(new CouponCartLine(product.Id, product.CategoryId, product.Name, variant.Price * item.Quantity));
        }

        return (lines, null);
    }

    private void LogEvaluation(string code, int? productId, int? categoryId,
        IReadOnlyCollection<CouponCartLine> cart, CouponEvaluation evaluation)
    {
        logger.LogInformation(
            "Coupon {CouponCode} evaluated. Coupon product {CouponProductId}, coupon category {CouponCategoryId}, cart products {CartProductIds}, cart categories {CartCategoryIds}, eligible {EligibilityResult}, rejection {RejectionReason}, eligible subtotal {EligibleSubtotal}, calculated discount {CalculatedDiscount}",
            code, productId, categoryId, cart.Select(line => line.ProductId).ToArray(),
            cart.Select(line => line.CategoryId).ToArray(), evaluation.IsEligible,
            evaluation.RejectionReason, evaluation.EligibleSubtotal, evaluation.DiscountAmount);
    }
}
