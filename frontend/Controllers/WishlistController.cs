using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class WishlistController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(Index), "Wishlist")
            });
        }

        try
        {
            var products = await api.GetWishlistAsync();
            await Task.WhenAll(products.Select(AttachRatingAsync));
            return View(products);
        }
        catch (HttpRequestException ex)
        {
            ViewBag.Error = ex.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "The wishlist endpoint is not available in the backend yet. Restart the backend service, then try again."
                : $"Could not load your wishlist. {ex.Message}";
            return View(new List<ProductViewModel>());
        }
    }

    private async Task AttachRatingAsync(ProductViewModel product)
    {
        try
        {
            var summary = await api.GetProductReviewsAsync(product.Id, page: 1, pageSize: 1);
            product.AverageRating = summary.AverageRating;
            product.ReviewCount = summary.ReviewCount;
        }
        catch (HttpRequestException)
        {
            // Keep the wishlist available if an individual rating summary is unavailable.
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, bool isWishlisted, string? returnUrl)
    {
        var isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";
        if (User.Identity?.IsAuthenticated != true)
        {
            if (isAjax) return Unauthorized(new { success = false, message = "Please sign in to save products." });
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = string.IsNullOrWhiteSpace(returnUrl)
                    ? Url.Action("Details", "Products", new { id = productId })
                    : returnUrl
            });
        }

        var product = await api.GetProductAsync(productId);
        if (product is null) return NotFound();

        bool success;
        try
        {
            success = isWishlisted
                ? await api.RemoveFromWishlistAsync(productId)
                : await api.AddToWishlistAsync(productId);
        }
        catch (HttpRequestException ex)
        {
            if (isAjax) return StatusCode(502, new { success = false, message = "Could not update your wishlist. Please try again." });
            TempData["WishlistError"] = $"Could not update your wishlist. {ex.Message}";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        TempData[success ? "WishlistMessage" : "WishlistError"] = success
            ? isWishlisted ? "Removed from your wishlist." : "Added to your wishlist."
            : "Could not update your wishlist. Please sign in again and try once more.";

        if (isAjax) return Json(new { success, isWishlisted = success ? !isWishlisted : isWishlisted, message = success ? (isWishlisted ? "Removed from your wishlist." : "Added to your wishlist.") : "Could not update your wishlist." });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Details", "Products", new { id = productId });
    }
}
