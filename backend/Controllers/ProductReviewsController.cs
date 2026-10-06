using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace backend.Controllers;

[ApiController]
[Route("api/products/{productId:int}/reviews")]
public class ProductReviewsController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(x => x.Id == productId))
            return NotFound();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var query = db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId);
        var count = await query.CountAsync();
        var average = count == 0 ? 0 : Math.Round(await query.AverageAsync(x => (double)x.Rating), 1);

        var breakdown = await query
            .GroupBy(x => x.Rating)
            .Select(x => new { Rating = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Rating, x => x.Count);

        var reviews = await (
            from review in query
            join customer in db.Customers.AsNoTracking() on review.CustomerId equals customer.Id
            orderby review.CreatedDate descending
            select new
            {
                review.Id,
                review.Rating,
                review.Title,
                review.Comment,
                CustomerName = customer.Name,
                review.CreatedDate,
                Images = db.ProductReviewImages.Where(image => image.ProductReviewId == review.Id).Select(image => image.ImageUrl).ToList(),
                VerifiedPurchase = db.Orders.Any(order =>
                    order.CustomerId == review.CustomerId &&
                    (order.PaymentStatus == "Paid" ||
                     order.Status == "Delivered" ||
                     order.Status == "Completed") &&
                    order.Items.Any(item => item.ProductId == productId))
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var canReview = false;
        var hasReviewed = false;

        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            hasReviewed = await db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.CustomerId == customerId);
            canReview = !hasReviewed && await db.Orders.AnyAsync(order =>
                order.CustomerId == customerId &&
                (order.PaymentStatus == "Paid" ||
                 order.Status == "Delivered" ||
                 order.Status == "Completed") &&
                order.Items.Any(item => item.ProductId == productId));
        }

        return Ok(new
        {
            averageRating = average,
            reviewCount = count,
            ratingBreakdown = breakdown,
            reviews,
            canReview,
            hasReviewed,
            page,
            pageSize,
            totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize))
        });
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(27_000_000)]
    public async Task<IActionResult> Create(
        int productId,
        [FromForm(Name = "Rating")] int rating,
        [FromForm(Name = "Title")] string? title,
        [FromForm(Name = "Comment")] string? comment,
        [FromForm(Name = "Images")] List<IFormFile>? images)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(x => x.Id == productId))
            return NotFound("Product not found.");

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        if (rating is < 1 or > 5)
            return BadRequest("Rating must be between 1 and 5.");

        title = (title ?? string.Empty).Trim();
        comment = (comment ?? string.Empty).Trim();

        if (title.Length > 100 || comment.Length > 2000)
            return BadRequest("Review is too long.");

        images ??= [];
        if (images.Count > 5) return BadRequest("Upload no more than five review images.");
        if (images.Any(file => file.Length == 0 || file.Length > 5_000_000 || !IsAllowedImage(file)))
            return BadRequest("Review images must be JPG, PNG, or WebP files under 5 MB each.");

        var purchased = await db.Orders.AnyAsync(order =>
            order.CustomerId == customerId &&
            (order.PaymentStatus == "Paid" ||
             order.Status == "Delivered" ||
             order.Status == "Completed") &&
            order.Items.Any(item => item.ProductId == productId));

        if (!purchased)
            return Forbid();

        if (await db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.CustomerId == customerId))
            return Conflict("You have already reviewed this product.");

        var review = new ProductReview
        {
            ProductId = productId,
            CustomerId = customerId,
            Rating = rating,
            Title = title,
            Comment = comment
        };
        db.ProductReviews.Add(review);

        var savedPaths = new List<string>();
        try
        {
            var uploadRoot = Path.GetFullPath(Path.Combine(config["UploadsPath"] ?? @"C:\UbhayaFashions\Uploads", "reviews", productId.ToString()));
            Directory.CreateDirectory(uploadRoot);
            foreach (var file in images)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadRoot, fileName);
                await using var stream = System.IO.File.Create(fullPath);
                await file.CopyToAsync(stream);
                savedPaths.Add(fullPath);
                review.Images.Add(new ProductReviewImage { ImageUrl = $"uploads/reviews/{productId}/{fileName}" });
            }
        }
        catch
        {
            foreach (var path in savedPaths) if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            throw;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            foreach (var path in savedPaths) if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            return Conflict("You have already reviewed this product.");
        }

        return Created(string.Empty, new { success = true });
    }

    private static bool IsAllowedImage(IFormFile file)
        => new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
}
