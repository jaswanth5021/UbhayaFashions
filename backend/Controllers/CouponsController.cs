using System.Security.Claims;
using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/coupons")]
[Authorize(Roles = "Customer")]
public class CouponsController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("checkout-offers")]
    public async Task<IActionResult> CheckoutOffers(CheckoutCouponOffersRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();
        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 100 ||
            request.Items.Any(item => item.Quantity <= 0 || item.Quantity > 100 || string.IsNullOrWhiteSpace(item.Size)))
            return BadRequest("A valid cart is required.");

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Include(product => product.Variants)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();
        if (products.Count != productIds.Count)
            return BadRequest("One or more products in your cart are no longer available.");

        var cartLines = new List<(int ProductId, int CategoryId, decimal Amount)>();
        foreach (var item in request.Items)
        {
            var product = products.Single(product => product.Id == item.ProductId);
            var hasColorVariants = product.Variants.Any(variant => !string.IsNullOrWhiteSpace(variant.Color));
            var sizeVariants = product.Variants
                .Where(candidate => string.Equals(candidate.Size, item.Size, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Match the storefront cart's variant resolution: prefer the selected
            // colour, but allow a shared (colour-neutral) size variant as fallback.
            var variant = hasColorVariants
                ? sizeVariants.FirstOrDefault(candidate =>
                    !string.IsNullOrWhiteSpace(item.Color) &&
                    string.Equals(candidate.Color, item.Color, StringComparison.OrdinalIgnoreCase))
                    ?? sizeVariants.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.Color))
                : sizeVariants.FirstOrDefault();
            if (variant is null || variant.Stock < item.Quantity)
                return BadRequest($"{product.Name} ({item.Size}) is no longer available in that quantity.");

            cartLines.Add((product.Id, product.CategoryId, variant.Price * item.Quantity));
        }

        var subtotal = cartLines.Sum(line => line.Amount);
        var now = DateTime.UtcNow;
        var coupons = await db.Coupons.AsNoTracking()
            .Where(coupon => coupon.IsActive &&
                (!coupon.StartsAtUtc.HasValue || coupon.StartsAtUtc <= now) &&
                (!coupon.EndsAtUtc.HasValue || coupon.EndsAtUtc >= now) &&
                (!coupon.MaximumUses.HasValue || coupon.UsageCount < coupon.MaximumUses))
            .ToListAsync();
        var hasPreviousOrder = await db.Orders.AnyAsync(order => order.CustomerId == customerId &&
            (order.PaymentStatus == "Paid" || order.Status == "Processing" ||
             order.Status == "Shipped" || order.Status == "Delivered"));
        var redeemedCouponIds = await db.CouponRedemptions
            .Where(redemption => redemption.CustomerId == customerId)
            .Select(redemption => redemption.CouponId)
            .ToListAsync();

        var offers = coupons.Select(coupon =>
        {
            var eligibleSubtotal = cartLines
                .Where(line => (!coupon.ProductId.HasValue || line.ProductId == coupon.ProductId.Value) &&
                               (!coupon.CategoryId.HasValue || line.CategoryId == coupon.CategoryId.Value))
                .Sum(line => line.Amount);
            var usageAllowed = !coupon.PerCustomerLimit.HasValue ||
                redeemedCouponIds.Count(id => id == coupon.Id) < coupon.PerCustomerLimit.Value;
            var firstOrderAllowed = !coupon.FirstOrderOnly || !hasPreviousOrder;
            var discount = coupon.DiscountType == "Percentage"
                ? eligibleSubtotal * coupon.DiscountValue / 100m
                : coupon.DiscountValue;
            if (coupon.MaximumDiscount.HasValue) discount = Math.Min(discount, coupon.MaximumDiscount.Value);
            discount = Math.Round(Math.Clamp(discount, 0m, eligibleSubtotal), 2, MidpointRounding.AwayFromZero);

            var cartMatchesProductScope = !coupon.ProductId.HasValue ||
                cartLines.All(line => line.ProductId == coupon.ProductId.Value);
            var cartMatchesCategoryScope = !coupon.CategoryId.HasValue ||
                cartLines.All(line => line.CategoryId == coupon.CategoryId.Value);
            var cartMatchesScope = cartMatchesProductScope && cartMatchesCategoryScope;
            var minimumOrderMet = eligibleSubtotal >= coupon.MinimumOrderAmount;
            var canApply = cartMatchesScope && eligibleSubtotal > 0m && minimumOrderMet && usageAllowed && firstOrderAllowed && discount > 0m;
            var eligibleProductName = coupon.ProductId.HasValue
                ? products.FirstOrDefault(product => product.Id == coupon.ProductId.Value)?.Name
                : null;
            var eligibilityMessage = !cartMatchesProductScope
                ? $"This coupon applies only to {eligibleProductName ?? "the eligible product"}. Remove other products from your cart to use this coupon."
                : !cartMatchesCategoryScope
                    ? "This coupon applies only to products in the eligible category. Remove products from other categories to use it."
                    : !minimumOrderMet
                    ? $"Add ₹{Math.Max(0m, coupon.MinimumOrderAmount - eligibleSubtotal):0.##} more in eligible items to use this offer."
                    : !usageAllowed
                    ? "You have already used this offer."
                    : !firstOrderAllowed
                        ? "This offer is for your first order only."
                        : discount <= 0m
                            ? "This offer does not add a discount to your current cart."
                            : string.Empty;

            return new { Coupon = coupon, EligibleSubtotal = eligibleSubtotal, UsageAllowed, FirstOrderAllowed,
                Discount = discount, CanApply = canApply, EligibilityMessage = eligibilityMessage };
        })
        .Where(offer => offer.EligibleSubtotal > 0m)
        .OrderByDescending(offer => offer.Discount)
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
            discountAmount = offer.CanApply ? offer.Discount : 0m,
            canApply = offer.CanApply,
            eligibilityMessage = offer.EligibilityMessage,
            offer.Coupon.MinimumOrderAmount,
            offer.Coupon.IsAutomatic,
            offer.Coupon.FirstOrderOnly,
            appliesTo = offer.Coupon.ProductId.HasValue ? "Selected product" :
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
            .Where(x => x.Id == productId)
            .Select(x => new { x.Id, x.CategoryId })
            .FirstOrDefaultAsync();
        if (product is null) return NotFound();

        var now = DateTime.UtcNow;
        var offers = await db.Coupons.AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => (!x.StartsAtUtc.HasValue || x.StartsAtUtc <= now) &&
                        (!x.EndsAtUtc.HasValue || x.EndsAtUtc >= now))
            .Where(x => !x.MaximumUses.HasValue || x.UsageCount < x.MaximumUses)
            .Where(x => (!x.ProductId.HasValue && !x.CategoryId.HasValue) ||
                        x.ProductId == product.Id || x.CategoryId == product.CategoryId)
            .OrderByDescending(x => x.DiscountType == "Percentage" ? x.DiscountValue : 0)
            .ThenBy(x => x.MinimumOrderAmount)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.DiscountType,
                x.DiscountValue,
                x.MaximumDiscount,
                x.MinimumOrderAmount,
                x.IsAutomatic,
                x.FirstOrderOnly,
                x.ProductId,
                x.CategoryId,
                x.EndsAtUtc
            })
            .Take(8)
            .ToListAsync();

        return Ok(offers.Select(x => new
        {
            x.Id,
            code = x.IsAutomatic ? null : x.Code,
            x.Name,
            discountText = x.DiscountType == "Percentage"
                ? $"{x.DiscountValue:0.##}% OFF" + (x.MaximumDiscount.HasValue ? $" up to ₹{x.MaximumDiscount.Value:0.##}" : "")
                : $"₹{x.DiscountValue:0.##} OFF",
            x.MinimumOrderAmount,
            discountType = x.DiscountType,
            discountValue = x.DiscountValue,
            maximumDiscount = x.MaximumDiscount,
            productId = x.ProductId,
            categoryId = x.CategoryId,
            x.IsAutomatic,
            x.FirstOrderOnly,
            appliesTo = x.ProductId.HasValue ? "This product" :
                        x.CategoryId.HasValue ? "Selected category" : "All products",
            x.EndsAtUtc,
            terms = string.Join(" • ", new[]
            {
                x.MinimumOrderAmount > 0 ? $"Minimum order ₹{x.MinimumOrderAmount:0.##}" : null,
                x.MaximumDiscount.HasValue && x.DiscountType == "Percentage" ? $"Maximum discount ₹{x.MaximumDiscount.Value:0.##}" : null,
                x.FirstOrderOnly ? "First order only" : null,
                x.IsAutomatic ? "Applied automatically at checkout" : "Enter the code at checkout"
            }.Where(value => value is not null))
        }));
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(CouponApplyRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        if (request.Subtotal < 0 || request.ProductIds is null || request.ProductIds.Count == 0)
            return BadRequest("A valid cart is required.");
        var now = DateTime.UtcNow;
        var normalizedCode = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var coupons = await db.Coupons.AsNoTracking()
            .Where(x => x.IsActive && (x.IsAutomatic || x.Code == normalizedCode))
            .Where(x => (!x.StartsAtUtc.HasValue || x.StartsAtUtc <= now) && (!x.EndsAtUtc.HasValue || x.EndsAtUtc >= now))
            .Where(x => !x.MaximumUses.HasValue || x.UsageCount < x.MaximumUses)
            .ToListAsync();
        var productData = await db.Products.AsNoTracking()
            .Where(x => request.ProductIds.Contains(x.Id))
            .Select(x => new { x.Id, x.CategoryId }).ToListAsync();
        var isFirstOrder = !await db.Orders.AnyAsync(x => x.CustomerId == customerId &&
            (x.PaymentStatus == "Paid" || x.Status == "Processing" || x.Status == "Shipped" || x.Status == "Delivered"));
        var usedIds = await db.CouponRedemptions.Where(x => x.CustomerId == customerId)
            .Select(x => x.CouponId).ToListAsync();
        var eligible = coupons.Where(c => request.Subtotal >= c.MinimumOrderAmount &&
            (!c.FirstOrderOnly || isFirstOrder) &&
            (!c.PerCustomerLimit.HasValue || usedIds.Count(id => id == c.Id) < c.PerCustomerLimit) &&
            (!c.ProductId.HasValue || productData.All(p => p.Id == c.ProductId.Value)) &&
            (!c.CategoryId.HasValue || productData.All(p => p.CategoryId == c.CategoryId.Value)) &&
            (c.IsAutomatic || string.Equals(c.Code, request.Code?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Select(c => new { Coupon = c, EligibleSubtotal = request.Subtotal })
            .ToList();
        if (eligible.Count == 0)
        {
            var matchingCoupon = coupons.FirstOrDefault(c =>
                !c.IsAutomatic && string.Equals(c.Code, request.Code?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchingCoupon is not null && request.Subtotal < matchingCoupon.MinimumOrderAmount)
                return BadRequest(new
                {
                    message = $"This coupon requires a minimum order value of ₹{matchingCoupon.MinimumOrderAmount:0.##}. Your current order subtotal is ₹{request.Subtotal:0.##}."
                });

            return BadRequest(new { message = "This coupon is invalid or does not apply to your cart." });
        }
        var chosen = eligible.OrderByDescending(x => Calculate(x.Coupon, x.EligibleSubtotal)).First();
        var discount = Calculate(chosen.Coupon, chosen.EligibleSubtotal);
        return Ok(new { code = chosen.Coupon.IsAutomatic ? (string?)null : chosen.Coupon.Code,
            name = chosen.Coupon.Name, discountAmount = discount, subtotal = request.Subtotal,
            payableSubtotal = Math.Max(0m, request.Subtotal - discount), isAutomatic = chosen.Coupon.IsAutomatic });
    }

    private static decimal Calculate(backend.Models.Coupon coupon, decimal subtotal)
    {
        var value = coupon.DiscountType == "Percentage"
            ? subtotal * coupon.DiscountValue / 100m : coupon.DiscountValue;
        if (coupon.MaximumDiscount.HasValue) value = Math.Min(value, coupon.MaximumDiscount.Value);
        return Math.Round(Math.Clamp(value, 0m, subtotal), 2, MidpointRounding.AwayFromZero);
    }
}
