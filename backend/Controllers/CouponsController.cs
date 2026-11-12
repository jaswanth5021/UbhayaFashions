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
            (!c.ProductId.HasValue || productData.Any(p => p.Id == c.ProductId)) &&
            (!c.CategoryId.HasValue || productData.Any(p => p.CategoryId == c.CategoryId)) &&
            (c.IsAutomatic || string.Equals(c.Code, request.Code?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Select(c => new { Coupon = c, EligibleSubtotal = request.Subtotal })
            .ToList();
        if (eligible.Count == 0) return BadRequest(new { message = "This coupon is invalid or does not apply to your cart." });
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
