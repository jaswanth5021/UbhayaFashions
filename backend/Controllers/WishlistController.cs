using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();

        var products = await db.Products
            .AsNoTracking()
            .Where(product => db.WishlistItems.Any(item =>
                item.CustomerId == customerId && item.ProductId == product.Id))
            .Include(product => product.Images)
            .Include(product => product.CategoryNavigation)
            .Include(product => product.Variants)
            .ToListAsync();

        return Ok(products);
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        if (!await db.Products.AnyAsync(product => product.Id == productId)) return NotFound();

        var exists = await db.WishlistItems.AnyAsync(item =>
            item.CustomerId == customerId && item.ProductId == productId);
        if (!exists)
        {
            db.WishlistItems.Add(new WishlistItem
            {
                CustomerId = customerId,
                ProductId = productId
            });
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();

        var item = await db.WishlistItems.FirstOrDefaultAsync(entry =>
            entry.CustomerId == customerId && entry.ProductId == productId);
        if (item is not null)
        {
            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    private bool TryGetCustomerId(out int customerId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);
}
