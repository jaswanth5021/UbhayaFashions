using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Roles = "Admin")]
public class AdminCouponsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All() => Ok(await db.Coupons.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CouponRequest request)
    {
        var error = await Validate(request);
        if (error is not null) return BadRequest(error);
        var code = request.Code.Trim().ToUpperInvariant();
        if (!request.IsAutomatic && await db.Coupons.AnyAsync(x => x.Code == code)) return Conflict("A coupon with this code already exists.");
        var coupon = Map(request, new Coupon());
        coupon.Code = request.IsAutomatic ? $"AUTO-{Guid.NewGuid():N}"[..13] : code;
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(All), new { id = coupon.Id }, coupon);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CouponRequest request)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();
        var error = await Validate(request, id);
        if (error is not null) return BadRequest(error);
        var code = request.Code.Trim().ToUpperInvariant();
        if (!request.IsAutomatic && await db.Coupons.AnyAsync(x => x.Id != id && x.Code == code)) return Conflict("A coupon with this code already exists.");
        Map(request, coupon);
        coupon.Code = request.IsAutomatic ? coupon.Code : code;
        await db.SaveChangesAsync();
        return Ok(coupon);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();
        if (await db.CouponRedemptions.AnyAsync(x => x.CouponId == id)) { coupon.IsActive = false; await db.SaveChangesAsync(); return NoContent(); }
        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> Validate(CouponRequest r, int? id = null)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 120) return "Enter a coupon name (maximum 120 characters).";
        if (!r.IsAutomatic && (string.IsNullOrWhiteSpace(r.Code) || r.Code.Length > 40)) return "Enter a valid coupon code.";
        if (r.DiscountType is not ("Percentage" or "Fixed")) return "Discount type must be Percentage or Fixed.";
        if (r.DiscountValue <= 0 || (r.DiscountType == "Percentage" && r.DiscountValue > 100)) return "Enter a valid discount value.";
        if ((r.MaximumDiscount.HasValue && r.MaximumDiscount.Value < 0) || r.MinimumOrderAmount < 0 || (r.MaximumUses.HasValue && r.MaximumUses.Value < 1) || (r.PerCustomerLimit.HasValue && r.PerCustomerLimit.Value < 1)) return "Discount limits must be positive.";
        if (r.StartsAtUtc.HasValue && r.EndsAtUtc.HasValue && r.EndsAtUtc <= r.StartsAtUtc) return "End date must be after start date.";
        if (r.ProductId.HasValue && !await db.Products.AnyAsync(x => x.Id == r.ProductId)) return "Selected product was not found.";
        if (r.CategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == r.CategoryId)) return "Selected category was not found.";
        return null;
    }

    private static Coupon Map(CouponRequest r, Coupon c)
    {
        c.Name = r.Name.Trim(); c.DiscountType = r.DiscountType; c.DiscountValue = r.DiscountValue;
        c.MaximumDiscount = r.MaximumDiscount; c.MinimumOrderAmount = r.MinimumOrderAmount;
        c.MaximumUses = r.MaximumUses; c.PerCustomerLimit = r.PerCustomerLimit;
        c.StartsAtUtc = r.StartsAtUtc; c.EndsAtUtc = r.EndsAtUtc; c.IsActive = r.IsActive;
        c.IsAutomatic = r.IsAutomatic; c.FirstOrderOnly = r.FirstOrderOnly;
        c.ProductId = r.ProductId; c.CategoryId = r.CategoryId;
        return c;
    }
}
