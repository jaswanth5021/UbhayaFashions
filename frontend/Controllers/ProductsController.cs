using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
