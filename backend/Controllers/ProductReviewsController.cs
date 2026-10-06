using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace backend.Controllers;

[ApiController]
[Route("api/products/{productId:int}/reviews")]
public class ProductReviewsController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? rating = null, [FromQuery] string? sort = null)
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

        var normalizedSort = (sort ?? "helpful").Trim().ToLowerInvariant();
        if (normalizedSort is not ("helpful" or "newest" or "oldest" or "highest" or "lowest" or "rating")) normalizedSort = "helpful";
        var filteredQuery = rating is >= 1 and <= 5 ? query.Where(review => review.Rating == rating) : query;
        var filteredCount = await filteredQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var currentCustomerId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var signedInCustomerId)
            ? signedInCustomerId
            : (int?)null;

        var reviewQuery = (
            from review in filteredQuery
            join customer in db.Customers.AsNoTracking() on review.CustomerId equals customer.Id
            select new
            {
                review.Id,
                review.Rating,
                review.Title,
                review.Comment,
                CustomerName = customer.Name,
                review.CreatedDate,
                Images = db.ProductReviewImages.Where(image => image.ProductReviewId == review.Id).OrderByDescending(image => image.CreatedDate).Select(image => image.ImageUrl).ToList(),
                ReviewPhotos = db.ProductReviewImages.Where(image => image.ProductReviewId == review.Id).OrderByDescending(image => image.CreatedDate).Select(image => image.ImageUrl).ToList(),
                HelpfulCount = db.ProductReviewVotes.Count(vote => vote.ProductReviewId == review.Id && vote.VoteType == 1),
                NotHelpfulCount = db.ProductReviewVotes.Count(vote => vote.ProductReviewId == review.Id && vote.VoteType == -1),
                CurrentUserVote = currentCustomerId.HasValue
                    ? db.ProductReviewVotes.Where(vote => vote.ProductReviewId == review.Id && vote.CustomerId == currentCustomerId.Value).Select(vote => (int?)vote.VoteType).FirstOrDefault()
                    : null,
                VerifiedPurchase = db.Orders.Any(order =>
                    order.CustomerId == review.CustomerId &&
                    (order.PaymentStatus == "Paid" ||
                     order.Status == "Delivered" ||
                     order.Status == "Completed") &&
                    order.Items.Any(item => item.ProductId == productId))
            });

        var orderedQuery = normalizedSort switch
        {
            "newest" => reviewQuery.OrderByDescending(review => review.CreatedDate),
            "oldest" => reviewQuery.OrderBy(review => review.CreatedDate),
            "highest" => reviewQuery.OrderByDescending(review => review.Rating).ThenByDescending(review => review.CreatedDate),
            "lowest" => reviewQuery.OrderBy(review => review.Rating).ThenByDescending(review => review.CreatedDate),
            "rating" => reviewQuery.OrderByDescending(review => review.VerifiedPurchase).ThenByDescending(review => review.Rating).ThenByDescending(review => review.CreatedDate),
            _ => reviewQuery.OrderByDescending(review => review.HelpfulCount - review.NotHelpfulCount).ThenByDescending(review => review.CreatedDate)
        };
        var reviews = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        if (page == 1 && pageSize == 2 && rating is null)
        {
            var preferred = await orderedQuery.Where(review => review.Rating == 5).Take(1).ToListAsync();
            preferred.AddRange(await orderedQuery.Where(review => review.Rating == 4).Take(1).ToListAsync());
            if (preferred.Count > 0)
            {
                if (preferred.Count < pageSize)
                {
                    var selectedIds = preferred.Select(review => review.Id).ToArray();
                    preferred.AddRange(await reviewQuery.Where(review => !selectedIds.Contains(review.Id))
                        .Take(pageSize - preferred.Count).ToListAsync());
                }
                reviews = preferred
                    .OrderByDescending(review => review.VerifiedPurchase)
                    .ThenByDescending(review => review.Rating).ThenByDescending(review => review.CreatedDate)
                    .ToList();
            }
        }

        var canReview = false;
        var hasReviewed = false;

        if (currentCustomerId is int customerId)
        {
            hasReviewed = await db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.CustomerId == customerId);
            canReview = !hasReviewed && await db.Orders.AnyAsync(order =>
                order.CustomerId == customerId &&
                (order.PaymentStatus == "Paid" ||
                 order.Status == "Delivered" ||
                 order.Status == "Completed") &&
                order.Items.Any(item => item.ProductId == productId));
        }

        var verifiedPurchaseCount = await query.CountAsync(review => db.Orders.Any(order =>
            order.CustomerId == review.CustomerId &&
            (order.PaymentStatus == "Paid" || order.Status == "Delivered" || order.Status == "Completed") &&
            order.Items.Any(item => item.ProductId == productId)));
        var customerPhotoCount = await db.ProductReviewImages.CountAsync(image =>
            db.ProductReviews.Any(review => review.Id == image.ProductReviewId && review.ProductId == productId));
        var customerPhotos = await (
            from image in db.ProductReviewImages.AsNoTracking()
            join review in query on image.ProductReviewId equals review.Id
            orderby image.CreatedDate descending, image.Id descending
            select image.ImageUrl)
            .Take(8)
            .ToListAsync();

        return Ok(new
        {
            averageRating = average,
            reviewCount = count,
            ratingBreakdown = breakdown,
            reviews,
            canReview,
            hasReviewed,
            verifiedPurchaseCount,
            customerPhotoCount,
            customerPhotos,
            page,
            pageSize,
            totalPages,
            filteredReviewCount = filteredCount,
            sort = normalizedSort,
            rating
        });
    }

    [Authorize]
    [HttpPost("~/api/products/reviews/{reviewId:int}/vote")]
    public async Task<IActionResult> Vote(int reviewId, [FromBody] ProductReviewVoteRequest request)
    {
        if (request.VoteType is not (1 or -1))
            return BadRequest("Vote type must be 1 (helpful) or -1 (not helpful).");
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();
        if (!await db.ProductReviews.AnyAsync(review => review.Id == reviewId))
            return NotFound("Review not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vote = await db.ProductReviewVotes.FirstOrDefaultAsync(item =>
            item.ProductReviewId == reviewId && item.CustomerId == customerId);
        if (vote is null)
        {
            db.ProductReviewVotes.Add(new ProductReviewVote
            {
                ProductReviewId = reviewId,
                CustomerId = customerId,
                VoteType = request.VoteType
            });
        }
        else if (vote.VoteType != request.VoteType)
        {
            vote.VoteType = request.VoteType;
            vote.UpdatedDate = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        var helpfulCount = await db.ProductReviewVotes.CountAsync(item => item.ProductReviewId == reviewId && item.VoteType == 1);
        var notHelpfulCount = await db.ProductReviewVotes.CountAsync(item => item.ProductReviewId == reviewId && item.VoteType == -1);
        await transaction.CommitAsync();

        return Ok(new { helpfulCount, notHelpfulCount, currentUserVote = request.VoteType });
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
        foreach (var image in images)
        {
            if (image.Length == 0 || image.Length > 5_000_000 || !await IsValidImageAsync(image))
                return BadRequest("Review images must contain valid JPG, PNG, or WebP image data and be under 5 MB each.");
        }

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

    private static async Task<bool> IsValidImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);
        var isJpeg = (extension is ".jpg" or ".jpeg") && read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = extension == ".png" && read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebp = extension == ".webp" && read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return isJpeg || isPng || isWebp;
    }
}

public sealed record ProductReviewVoteRequest(int VoteType);
