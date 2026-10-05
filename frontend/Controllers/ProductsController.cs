using LadiesDressStore.Web.Models;
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
}
