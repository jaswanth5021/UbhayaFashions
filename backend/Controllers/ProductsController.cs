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
        [FromQuery] bool bestSellers = false,
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

        if (bestSellers)
        {
            query = query.Where(x => x.IsBestSeller);
        }

        query = sort?.Trim().ToLowerInvariant() switch
        {
            "price-low" => query.OrderBy(x => x.Variants.Min(variant => (decimal?)variant.Price) ?? decimal.MaxValue),
            "price-high" => query.OrderByDescending(x => x.Variants.Max(variant => (decimal?)variant.Price) ?? 0),
            "name" => query.OrderBy(x => x.Name),
            "newest" => query.OrderByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.IsBestSeller).ThenByDescending(x => x.Id)
        };

        return Ok(await query.ToListAsync());
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

    [HttpGet("GetNewarrivals")]
    public async Task<IActionResult> GetNewarrivals()
        => Ok(await db.Products.AsNoTracking()
            .Include(x => x.CategoryNavigation)
            .Include(x => x.Images)
            .Include(x => x.Videos)
            .Include(x => x.Variants)
            .OrderByDescending(x => x.Id)
            .Take(4)
            .ToListAsync());

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
