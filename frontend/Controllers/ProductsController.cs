using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class ProductsController(ApiService api) : Controller
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
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sort = null,
        int page = 1,
        bool bestSellers = false)
    {
        var products = await api.GetProductsAsync(
            search,
            category,
            size,
            minPrice,
            maxPrice,
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
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
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
            var bestSellers = (await api.GetProductsAsync())
                .Where(item => item.IsBestSeller)
                .ToList();

            var otherBestSellers = bestSellers
                .Where(item => item.Id != id)
                .Take(6)
                .ToList();

            ViewBag.BestSellers = otherBestSellers.Count > 0
                ? otherBestSellers
                : bestSellers.Where(item => item.Id == id).Take(1).ToList();
        }
        catch (HttpRequestException)
        {
            ViewBag.BestSellers = new List<LadiesDressStore.Web.Models.ProductViewModel>();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                ViewBag.IsWishlisted = (await api.GetWishlistAsync()).Any(item => item.Id == id);
            }
            catch (HttpRequestException)
            {
                ViewBag.IsWishlisted = false;
            }
        }

        return View(product);
    }
}
