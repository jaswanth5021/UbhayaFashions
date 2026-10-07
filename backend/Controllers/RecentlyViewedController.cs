using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/recently-viewed")]
[Authorize]
public class RecentlyViewedController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();

        var products = await db.RecentlyViewedProducts
            .AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .OrderByDescending(item => item.ViewedAtUtc)
            .Take(7)
            .Select(item => new
            {
                item.Product.Id,
                item.Product.Name,
                Category = item.Product.CategoryNavigation.Name,
                item.Product.ImageUrl,
                Price = item.Product.Variants.OrderBy(variant => variant.Price)
                    .Select(variant => (decimal?)variant.Price).FirstOrDefault() ?? 0m,
                Discount = item.Product.Variants.OrderBy(variant => variant.Price)
                    .Select(variant => (decimal?)variant.Discount).FirstOrDefault() ?? 0m,
                item.Product.IsBestSeller
            })
            .ToListAsync();

        var productIds = products.Select(product => product.Id).ToArray();
        var sizes = await db.ProductVariants.AsNoTracking()
            .Where(variant => productIds.Contains(variant.ProductId) && variant.Stock > 0)
            .OrderBy(variant => variant.Size)
            .Select(variant => new { variant.ProductId, variant.Size })
            .ToListAsync();
        var sizesByProductId = sizes.GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Size).Distinct().ToList());
        var reviews = await db.ProductReviews.AsNoTracking()
            .Where(review => productIds.Contains(review.ProductId))
            .GroupBy(review => review.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                AverageRating = group.Average(review => (decimal)review.Rating),
                ReviewCount = group.Count()
            })
            .ToDictionaryAsync(item => item.ProductId);

        return Ok(products.Select(product =>
        {
            reviews.TryGetValue(product.Id, out var reviewSummary);
            return new
            {
                product.Id,
                product.Name,
                product.Category,
                product.ImageUrl,
                product.Price,
                product.Discount,
                product.IsBestSeller,
                Sizes = sizesByProductId.GetValueOrDefault(product.Id) ?? [],
                AverageRating = reviewSummary?.AverageRating ?? 0,
                ReviewCount = reviewSummary?.ReviewCount ?? 0
            };
        }));
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Record(int productId)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        if (!await db.Products.AnyAsync(product => product.Id == productId)) return NotFound();

        var viewed = await db.RecentlyViewedProducts.FirstOrDefaultAsync(item =>
            item.CustomerId == customerId && item.ProductId == productId);
        if (viewed is null)
        {
            db.RecentlyViewedProducts.Add(new RecentlyViewedProduct
            {
                CustomerId = customerId,
                ProductId = productId,
                ViewedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            viewed.ViewedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    private bool TryGetCustomerId(out int customerId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);
}
