using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? size = null,
        [FromQuery] string? color = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? availability = null,
        [FromQuery] bool bestSellers = false,
        [FromQuery] bool newArrivals = false,
        [FromQuery] string? sort = null)
    {
        var query = db.Products
            .AsNoTracking()
            .Include(x => x.CategoryNavigation)
            .Include(x => x.Images)
            .Include(x => x.Videos)
            .Include(x => x.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            var normalizedSearch = search.ToLowerInvariant();

            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{search}%") ||
                EF.Functions.Like(x.Description, $"%{search}%") ||
                EF.Functions.Like(x.Colors, $"%{search}%") ||
                EF.Functions.Like(x.CategoryNavigation.Name, $"%{search}%") ||
                x.Variants.Any(variant =>
                    EF.Functions.Like(variant.Size.ToLower(), $"%{normalizedSearch}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(x => x.CategoryNavigation.Name == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            var normalizedSize = size.Trim();
            query = query.Where(x => x.Variants.Any(variant => variant.Size == normalizedSize && variant.Stock > 0));
        }

        if (!string.IsNullOrWhiteSpace(color))
        {
            var normalizedColor = color.Trim();
            query = query.Where(x => EF.Functions.Like(x.Colors, $"%{normalizedColor}%"));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Variants.Any(variant => variant.Price >= minPrice.Value));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Variants.Any(variant => variant.Price <= maxPrice.Value));
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            var normalizedAvailability = availability.Trim().ToLowerInvariant();

            if (normalizedAvailability == "in-stock")
            {
                query = query.Where(x => x.Variants.Any(variant => variant.Stock > 0));
            }
            else if (normalizedAvailability == "out-of-stock")
            {
                query = query.Where(x => !x.Variants.Any(variant => variant.Stock > 0));
            }
        }

        if (bestSellers)
        {
            query = query.Where(x => x.IsBestSeller);
        }

        if (newArrivals)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            query = query.Where(x => x.CreatedDate >= cutoff && x.Variants.Any(variant => variant.Stock > 0));
        }

        query = newArrivals ? query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id) : sort?.Trim().ToLowerInvariant() switch
        {
            "price-low" => query.OrderBy(x => x.Variants.Min(variant => (decimal?)variant.Price) ?? decimal.MaxValue),
            "price-high" => query.OrderByDescending(x => x.Variants.Max(variant => (decimal?)variant.Price) ?? 0),
            "name" => query.OrderBy(x => x.Name),
            "newest" => query.OrderByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.IsBestSeller).ThenByDescending(x => x.Id)
        };

        var products = await query.ToListAsync();
        return Ok(await AttachReviewSummariesAsync(products));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await db.Products
            .AsNoTracking()
            .Include(x => x.CategoryNavigation)
            .Include(x => x.Images)
            .Include(x => x.Videos)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:int}/related")]
    public async Task<IActionResult> GetRelated(int id)
    {
        var product = await db.Products.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.CategoryId, item.Colors })
            .FirstOrDefaultAsync();

        if (product is null) return NotFound();

        var categoryProducts = await db.Products.AsNoTracking()
            .Where(item => item.Id != id && item.CategoryId == product.CategoryId
                && item.Variants.Any(variant => variant.Stock > 0))
            .OrderByDescending(item => item.IsBestSeller)
            .ThenByDescending(item => item.Id)
            .Select(item => new RelatedProductResponse(
                item.Id, item.Name, item.CategoryNavigation.Name, item.ImageUrl,
                item.Variants.OrderBy(variant => variant.Price).Select(variant => (decimal?)variant.Price).FirstOrDefault() ?? 0,
                item.Variants.OrderBy(variant => variant.Price).Select(variant => (decimal?)variant.Discount).FirstOrDefault() ?? 0,
                item.IsBestSeller, item.Colors))
            .Take(6)
            .ToListAsync();

        var results = categoryProducts.ToList();
        if (results.Count < 4)
        {
            var existingIds = results.Select(item => item.Id).Append(id).ToList();
            var fallback = await db.Products.AsNoTracking()
                .Where(item => !existingIds.Contains(item.Id) && item.Variants.Any(variant => variant.Stock > 0))
                .OrderByDescending(item => item.IsBestSeller)
                .ThenByDescending(item => item.Id)
                .Select(item => new RelatedProductResponse(
                    item.Id, item.Name, item.CategoryNavigation.Name, item.ImageUrl,
                    item.Variants.OrderBy(variant => variant.Price).Select(variant => (decimal?)variant.Price).FirstOrDefault() ?? 0,
                    item.Variants.OrderBy(variant => variant.Price).Select(variant => (decimal?)variant.Discount).FirstOrDefault() ?? 0,
                    item.IsBestSeller, item.Colors))
                .Take(6 - results.Count)
                .ToListAsync();

            results.AddRange(fallback);
        }

        var cardDetails = await GetCardDetailsAsync(results.Select(item => item.Id).Distinct().ToArray());

        return Ok(results.Select(item => new
        {
            item.Id, item.Name, item.Category, item.ImageUrl, item.Price, item.Discount,
            item.IsBestSeller,
            AverageRating = cardDetails[item.Id].AverageRating,
            ReviewCount = cardDetails[item.Id].ReviewCount,
            Sizes = cardDetails[item.Id].Sizes
        }));
    }

    private async Task<Dictionary<int, ProductCardDetails>> GetCardDetailsAsync(int[] productIds)
    {
        var sizes = await db.ProductVariants.AsNoTracking()
            .Where(variant => productIds.Contains(variant.ProductId) && variant.Stock > 0)
            .OrderBy(variant => variant.Size)
            .Select(variant => new { variant.ProductId, variant.Size })
            .ToListAsync();
        var sizeLookup = sizes.GroupBy(item => item.ProductId)
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

        return productIds.ToDictionary(id => id, id =>
        {
            var productSizes = sizeLookup.GetValueOrDefault(id) ?? [];
            return reviews.TryGetValue(id, out var summary)
                ? new ProductCardDetails(productSizes, summary.AverageRating, summary.ReviewCount)
                : new ProductCardDetails(productSizes, 0, 0);
        });
    }

    private sealed record ProductCardDetails(List<string> Sizes, decimal AverageRating, int ReviewCount);

    private sealed record RelatedProductResponse(
        int Id, string Name, string Category, string ImageUrl, decimal Price,
        decimal Discount, bool IsBestSeller, string Colors);

    [HttpGet("GetNewarrivals")]
    public async Task<IActionResult> GetNewarrivals()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var products = await db.Products.AsNoTracking()
            .Include(x => x.CategoryNavigation)
            .Include(x => x.Images)
            .Include(x => x.Videos)
            .Include(x => x.Variants)
            .Where(x => x.CreatedDate >= cutoff && x.Variants.Any(variant => variant.Stock > 0))
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Take(4)
            .ToListAsync();

        return Ok(await AttachReviewSummariesAsync(products));
    }

    private async Task<List<Product>> AttachReviewSummariesAsync(List<Product> products)
    {
        if (products.Count == 0) return products;

        var productIds = products.Select(product => product.Id).ToArray();
        var summaries = await db.ProductReviews.AsNoTracking()
            .Where(review => productIds.Contains(review.ProductId))
            .GroupBy(review => review.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                AverageRating = group.Average(review => (decimal)review.Rating),
                ReviewCount = group.Count()
            })
            .ToDictionaryAsync(summary => summary.ProductId);

        foreach (var product in products)
        {
            if (!summaries.TryGetValue(product.Id, out var summary)) continue;
            product.AverageRating = Math.Round(summary.AverageRating, 1);
            product.ReviewCount = summary.ReviewCount;
        }

        return products;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        var category = product.CategoryId > 0
            ? await db.Categories.FindAsync(product.CategoryId)
            : await db.Categories.FirstOrDefaultAsync(item => item.Name == product.Category);

        if (category is null) return BadRequest("Choose a valid category.");

        product.CategoryId = category.Id;
        product.CategoryNavigation = category;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Product input)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var category = input.CategoryId > 0
            ? await db.Categories.FindAsync(input.CategoryId)
            : await db.Categories.FirstOrDefaultAsync(item => item.Name == input.Category);

        if (category is null) return BadRequest("Choose a valid category.");

        product.Name = input.Name;
        product.Description = input.Description;
        product.CategoryId = category.Id;
        product.CategoryNavigation = category;
        product.Colors = input.Colors;
        product.ImageUrl = input.ImageUrl;

        await db.SaveChangesAsync();
        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
