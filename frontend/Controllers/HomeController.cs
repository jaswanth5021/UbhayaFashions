using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class HomeController(ApiService api, ILogger<HomeController> logger) : Controller
{
    public IActionResult Privacy() => View();

    public IActionResult Terms() => View();

    public IActionResult About() => View();

    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    public async Task<IActionResult> Index()
    {
        // These requests are independent; start them together so their
        // network/database latency does not accumulate on the homepage.
        var productsTask = LoadNewArrivalsAsync();
        var categoriesTask = LoadHomeCategoriesAsync();
        var wishlistTask = User.Identity?.IsAuthenticated == true
            ? LoadWishlistedProductIdsAsync()
            : Task.FromResult(new HashSet<int>());

        var (products, catalogUnavailable) = await productsTask;
        ViewBag.HomeCategories = await categoriesTask;
        ViewBag.WishlistedProductIds = await wishlistTask;
        ViewBag.CatalogUnavailable = catalogUnavailable;

        return View(products);
    }

    private async Task<(List<LadiesDressStore.Web.Models.ProductViewModel> Products, bool Unavailable)> LoadNewArrivalsAsync()
    {
        try
        {
            return (await api.GetNewarrivalsAsync(), false);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "The product API is unavailable while loading the home page.");
            return ([], true);
        }
        catch (System.Text.Json.JsonException exception)
        {
            logger.LogWarning(exception, "The product API returned invalid data while loading the home page.");
            return ([], true);
        }
    }

    private async Task<List<LadiesDressStore.Web.Models.CategoryViewModel>> LoadHomeCategoriesAsync()
    {
        try
        {
            return await api.GetHomeCategoriesAsync();
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    private async Task<HashSet<int>> LoadWishlistedProductIdsAsync()
    {
        try
        {
            return (await api.GetWishlistAsync())
                .Select(product => product.Id)
                .ToHashSet();
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}
