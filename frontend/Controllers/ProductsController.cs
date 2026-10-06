using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;

namespace LadiesDressStore.Web.Controllers;

public class ProductsController(ApiService api, ILogger<ProductsController> logger) : Controller
{
    public async Task<IActionResult> Categories()
    {
        var categories = await api.GetCategoriesAsync();
        return View(categories);
    }

    public async Task<IActionResult> Index(
        string? search = null,
        string? category = null,
        string? size = null,
        string? color = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? availability = null,
        string? sort = null,
        int page = 1,
        bool bestSellers = false)
    {
        var products = await api.GetProductsAsync(
            search,
            category,
            size,
            color,
            minPrice,
            maxPrice,
            availability,
            bestSellers,
            sort);

        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                ViewBag.WishlistedProductIds = (await api.GetWishlistAsync())
                    .Select(product => product.Id)
                    .ToHashSet();
            }
            catch (HttpRequestException)
            {
                ViewBag.WishlistedProductIds = new HashSet<int>();
            }
        }

        try
        {
            ViewBag.Categories = await api.GetCategoriesAsync();
        }
        catch (HttpRequestException)
        {
            ViewBag.Categories = new List<CategoryViewModel>();
        }

        const int pageSize = 12;
        var productCount = products.Count;
        var pageCount = Math.Max(1, (int)Math.Ceiling(productCount / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        products = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Size = size;
        ViewBag.Color = color;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Availability = availability;
        ViewBag.Sort = sort;
        ViewBag.BestSellersOnly = bestSellers;
        ViewBag.ProductCount = productCount;
        ViewBag.CurrentPage = page;
        ViewBag.PageCount = pageCount;

        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await api.GetProductAsync(id);
        if (product is null) return NotFound();

        try
        {
            ViewBag.RelatedProducts = (await api.GetRelatedProductsAsync(id))
                .Where(item => item.Id != id)
                .DistinctBy(item => item.Id)
                .Take(6)
                .ToList();
        }
        catch (HttpRequestException)
        {
            ViewBag.RelatedProducts = new List<RelatedProductViewModel>();
        }

        try
        {
            ViewBag.ProductReviews = await api.GetProductReviewsAsync(id);
        }
        catch (HttpRequestException ex)
        {
            ViewBag.ProductReviews = new ProductReviewsViewModel();
            ViewBag.ProductReviewsError = "Reviews are temporarily unavailable. Please try again later.";
            logger.LogWarning(ex, "Review API request failed for product {ProductId}", id);
        }
        catch (JsonException ex)
        {
            ViewBag.ProductReviews = new ProductReviewsViewModel();
            ViewBag.ProductReviewsError = "Reviews could not be loaded right now. Please try again later.";
            logger.LogError(ex, "Review API returned invalid data for product {ProductId}", id);
        }
        catch (OperationCanceledException ex)
        {
            ViewBag.ProductReviews = new ProductReviewsViewModel();
            ViewBag.ProductReviewsError = "Reviews are taking too long to load. Please try again later.";
            logger.LogWarning(ex, "Review API request timed out for product {ProductId}", id);
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var wishlistIds = (await api.GetWishlistAsync()).Select(item => item.Id).ToHashSet();
                ViewBag.WishlistedProductIds = wishlistIds;
                ViewBag.IsWishlisted = wishlistIds.Contains(id);
            }
            catch (HttpRequestException)
            {
                ViewBag.IsWishlisted = false;
            }
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoteReview(int productId, int reviewId, int voteType, string? returnUrl = null)
    {
        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : $"{Url.Action(nameof(Details), new { id = productId })}#productReviews";
        var loginUrl = Url.Action("Login", "Account", new
        {
            returnUrl = safeReturnUrl
        });

        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { requiresLogin = true, loginUrl });
        if (voteType is not (1 or -1))
            return BadRequest(new { message = "Choose helpful or not helpful." });

        try
        {
            var result = await api.VoteForProductReviewAsync(reviewId, voteType);
            if (result.Success && result.Result is not null)
                return Json(result.Result);
            if (result.Status == HttpStatusCode.Unauthorized)
                return Unauthorized(new { requiresLogin = true, loginUrl });
            return StatusCode((int)result.Status, new { message = "We couldn't save your vote. Please try again." });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Review vote request failed for review {ReviewId}", reviewId);
            return StatusCode(502, new { message = "Review voting is temporarily unavailable." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReviewPage(int productId, int page = 1, int? rating = null, string? sort = null)
    {
        try
        {
            return Json(await api.GetProductReviewsAsync(productId, page, 10, rating, sort ?? "helpful"));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Review page request failed for product {ProductId}, page {Page}", productId, page);
            return StatusCode(502, new { message = "Reviews are temporarily unavailable." });
        }
    }

    [HttpGet("Reviews/{productId:int}")]
    public async Task<IActionResult> Reviews(int productId, int? rating = null, string? sort = "helpful")
    {
        var product = await api.GetProductAsync(productId);
        if (product is null) return NotFound();
        try
        {
            ViewBag.ProductReviews = await api.GetProductReviewsAsync(productId, 1, 10, rating, sort);
        }
        catch (HttpRequestException ex)
        {
            ViewBag.ProductReviews = new ProductReviewsViewModel();
            ViewBag.ProductReviewsError = "Reviews are temporarily unavailable. Please try again later.";
            logger.LogWarning(ex, "Reviews page request failed for product {ProductId}", productId);
        }
        return View("Reviews", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(
        int productId,
        [FromForm, Bind(Prefix = "")] ProductReviewSubmissionViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var productUrl = Url.Action(nameof(Details), new { id = productId }) ?? $"/Products/Details/{productId}";
            var reviewUrl = productUrl + "#productReviews";
            return RedirectToAction("Login", "Account", new { returnUrl = reviewUrl });
        }

        var ratingState = ModelState[nameof(model.Rating)];
        if (model.Rating is < 1 or > 5 || ratingState?.Errors.Count > 0)
        {
            var rating = model.Rating;
            TempData["ReviewError"] = "Please select a rating between 1 and 5.";
            logger.LogWarning(
                "Review submission validation failed for product {ProductId}. Rating: {Rating}; errors: {Errors}",
                productId,
                rating,
                string.Join("; ", ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                    .Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value!.Errors.Select(error => error.ErrorMessage))}")));
            return RedirectToProductReviews(productId);
        }

        // A rating by itself is a valid review. Normalize blank optional fields
        // before sending them to the review API.
        model.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();
        model.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        model.Images ??= [];
        if (model.Images.Count > 5 || model.Images.Any(image => image.Length > 5_000_000))
        {
            TempData["ReviewError"] = "Choose up to five images, each under 5 MB.";
            return RedirectToProductReviews(productId);
        }

        try
        {
            var result = await api.SubmitProductReviewAsync(productId, model);
            if (result.Success)
                TempData["ReviewMessage"] = "Thank you. Your review has been submitted.";
            else
                TempData["ReviewError"] = result.Message.Trim('"');
        }
        catch (HttpRequestException ex)
        {
            TempData["ReviewError"] = "We couldn't reach the review service. Please try again.";
            logger.LogError(ex, "Review submission request failed for product {ProductId}", productId);
        }
        catch (OperationCanceledException ex)
        {
            TempData["ReviewError"] = "The review service took too long to respond. Please try again.";
            logger.LogWarning(ex, "Review submission timed out for product {ProductId}", productId);
        }

        return RedirectToProductReviews(productId);
    }

    private IActionResult RedirectToProductReviews(int productId)
    {
        var productUrl = Url.Action(nameof(Details), new { id = productId })
            ?? $"/Products/Details/{productId}";
        return Redirect($"{productUrl}#productReviews");
    }

}
