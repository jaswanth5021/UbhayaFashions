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
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(CouponApplyRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        if (request.Subtotal < 0 || request.ProductIds is null || request.ProductIds.Count == 0)
            return BadRequest("A valid cart is required.");
        var now = DateTime.UtcNow;
        var coupons = await db.Coupons.AsNoTracking()
            .Where(x => x.IsActive && (x.IsAutomatic || x.Code == request.Code!.Trim().ToUpper()))
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
