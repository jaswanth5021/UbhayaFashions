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

    public async Task<IActionResult> Index(string? category = null, int page = 1)
    {
        var products = await api.GetProductsAsync();

        if (!string.IsNullOrWhiteSpace(category))
        {
            products = products
                .Where(product => string.Equals(
                    product.Category?.Trim(),
                    category.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        const int pageSize = 12;
        var productCount = products.Count;
        var pageCount = Math.Max(1, (int)Math.Ceiling(productCount / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        products = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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

        ViewBag.Category = category;
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
            ViewBag.BestSellers = (await api.GetProductsAsync())
                .Where(item => item.Id != id)
                .Take(6)
                .ToList();
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
