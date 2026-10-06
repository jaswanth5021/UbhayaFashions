using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/products/{productId:int}/reviews")]
public class ProductReviewsController(ApplicationDbContext db) : ControllerBase
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
    public async Task<IActionResult> Create(int productId, ProductReviewRequest request)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(x => x.Id == productId))
            return NotFound("Product not found.");

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        if (request.Rating is < 1 or > 5)
            return BadRequest("Rating must be between 1 and 5.");

        var title = (request.Title ?? string.Empty).Trim();
        var comment = (request.Comment ?? string.Empty).Trim();

        if (title.Length > 100 || comment.Length > 2000)
            return BadRequest("Review is too long.");

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

        db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            CustomerId = customerId,
            Rating = request.Rating,
            Title = title,
            Comment = comment
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Conflict("You have already reviewed this product.");
        }

        return Created(string.Empty, new { success = true });
    }
}

public sealed record ProductReviewRequest(int Rating, string? Title, string? Comment);
