using System.Text.Json;
using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace LadiesDressStore.Web.Controllers;

public class CartController : Controller
{
    private readonly ApiService api;
    private readonly IMemoryCache productCache;

    [ActivatorUtilitiesConstructor]
    public CartController(ApiService api, IMemoryCache productCache)
    {
        this.api = api;
        this.productCache = productCache;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/Orders/Checkout" });
        }

        return RedirectToAction("Checkout", "Orders");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? size, int quantity = 1, bool buyNow = false)
    {
        var isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";
        var product = await api.GetProductAsync(productId);
        if (product is null) return isAjax ? NotFound(new { success = false, message = "Product not found." }) : NotFound();
        var normalizedSize = size?.Trim();
        var variant = product.Variants.FirstOrDefault(item =>
            string.Equals(item.Size, normalizedSize, StringComparison.OrdinalIgnoreCase));
        if (variant is null)
        {
            if (isAjax) return BadRequest(new { success = false, message = "Please select an available size." });
            TempData["CartError"] = "Please select an available size.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        if (variant.Stock < 1)
        {
            if (isAjax) return BadRequest(new { success = false, message = $"Size {variant.Size} is out of stock." });
            TempData["CartError"] = $"Size {variant.Size} is out of stock.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        if (quantity < 1)
        {
            if (isAjax) return BadRequest(new { success = false, message = "Please choose a quantity of at least 1." });
            TempData["CartError"] = "Please choose a quantity of at least 1.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        if (quantity > variant.Stock)
        {
            if (isAjax) return BadRequest(new { success = false, message = $"Only {variant.Stock} item(s) of size {variant.Size} are in stock." });
            TempData["CartError"] = $"Only {variant.Stock} item(s) of size {variant.Size} are in stock.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == productId &&
            string.Equals(x.Size, normalizedSize, StringComparison.OrdinalIgnoreCase));

        if ((item?.Quantity ?? 0) + quantity > variant.Stock)
        {
            if (isAjax) return BadRequest(new { success = false, message = $"Only {variant.Stock} item(s) of size {variant.Size} are in stock." });
            TempData["CartError"] = $"Only {variant.Stock} item(s) of size {variant.Size} are in stock. Your bag already contains {item!.Quantity}.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        if (item is null)
        {
            cart.Add(new CartItemViewModel
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = variant.Price,
                ImageUrl = product.ImageUrl,
                Size = variant.Size,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity += quantity;
        }

        SaveCart(cart);
        if (isAjax) return Json(new { success = true, message = $"{product.Name} added to your bag.", cartCount = cart.Sum(x => x.Quantity) });
        TempData["OpenCartDrawer"] = true;
        return RedirectToAction("Details", "Products", new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId, string? size)
    {
        var isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == productId && x.Size == size);
        SaveCart(cart);
        if (isAjax)
            return Json(new { success = true, cartCount = cart.Sum(x => x.Quantity), cartTotal = cart.Sum(x => x.Price * x.Quantity), isEmpty = cart.Count == 0 });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, string? currentSize, string? size, int quantity = 1, string? returnUrl = null)
    {
        IActionResult RedirectBack()
        {
            var target = returnUrl?.Split('?', 2)[0].TrimEnd('/');
            var isCartPage = string.Equals(target, "/Cart", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, "/Cart/Index", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) &&
                !isCartPage &&
            returnUrl != Url.Action(nameof(Index)))
            {
                TempData["OpenCartDrawer"] = true;
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == productId && string.Equals(x.Size, currentSize, StringComparison.OrdinalIgnoreCase));
        if (item is null) return RedirectBack();
        var product = await api.GetProductAsync(productId);
        var variant = product?.Variants.FirstOrDefault(x => string.Equals(x.Size, size?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (variant is null || variant.Stock < 1)
        {
            TempData["CartError"] = "That size is not available. Please choose another size.";
            return RedirectBack();
        }
        if (quantity < 1 || quantity > variant.Stock)
        {
            TempData["CartError"] = $"Choose a quantity from 1 to {variant.Stock} for size {variant.Size}.";
            return RedirectBack();
        }

        var sameVariant = cart.FirstOrDefault(x => x != item && x.ProductId == productId && string.Equals(x.Size, variant.Size, StringComparison.OrdinalIgnoreCase));
        if (sameVariant is not null)
        {
            if (quantity + sameVariant.Quantity > variant.Stock)
            {
                TempData["CartError"] = $"Only {variant.Stock} item(s) of size {variant.Size} are in stock.";
                return RedirectBack();
            }
            sameVariant.Quantity += quantity;
            cart.Remove(item);
        }
        else
        {
            item.Size = variant.Size;
            item.Quantity = quantity;
            item.Price = variant.Price;
        }
        SaveCart(cart);
        TempData["CartMessage"] = "Cart updated.";
        return RedirectBack();
    }

    private List<CartItemViewModel> GetCart()
    {
        var json = Request.Cookies["cart"];
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? [];
    }

    private void SaveCart(List<CartItemViewModel> cart)
    {
        Response.Cookies.Append(
            "cart",
            JsonSerializer.Serialize(cart),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
    }
}
