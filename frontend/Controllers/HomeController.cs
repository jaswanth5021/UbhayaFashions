using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class HomeController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        // These requests are independent; start them together so their
        // network/database latency does not accumulate on the homepage.
        var productsTask = api.GetNewarrivalsAsync();
        var categoriesTask = LoadHomeCategoriesAsync();
        var wishlistTask = User.Identity?.IsAuthenticated == true
            ? LoadWishlistedProductIdsAsync()
            : Task.FromResult(new HashSet<int>());

        var products = await productsTask;
        ViewBag.HomeCategories = await categoriesTask;
        ViewBag.WishlistedProductIds = await wishlistTask;

        return View(products);
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
