using System.Text.Json;
using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

[Authorize]
public class OrdersController(ApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.OrdersJson = await api.GetMyOrdersAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = ReadCart();
        if (cart.Count == 0)
            return RedirectToAction("Index", "Cart");

        var savedAddresses = new List<SavedAddressViewModel>();
        try
        {
            savedAddresses = await api.GetMyAddressesAsync();
        }
        catch (HttpRequestException) { }
        catch (JsonException) { }

        ViewBag.SavedAddresses = savedAddresses;
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ShippingAddress))
        {
            TempData["Error"] = "Delivery address is required.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = ReadCart();
        if (cart.Count == 0)
            return RedirectToAction("Index", "Cart");

        // Keep the existing COD flow intact.
        var result = await api.CreateOrderAsync(cart, model.ShippingAddress);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Index", "Cart");
        }

        Response.Cookies.Delete("cart");
        TempData["Success"] = "Order placed successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CreateOrderViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ShippingAddress))
        {
            TempData["Error"] = "Delivery address is required.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = ReadCart();
        if (cart.Count == 0)
            return RedirectToAction("Index", "Cart");

        var result = await api.CreateRazorpayOrderAsync(cart, model.ShippingAddress);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Index", "Cart");
        }

        return View("Payment", result.Data);
    }

    [HttpGet]
    public IActionResult Success(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }

    [HttpGet]
    public IActionResult Failure(int? id, string? message)
    {
        ViewBag.OrderId = id;
        ViewBag.FailureMessage = string.IsNullOrWhiteSpace(message)
            ? "Your payment was not completed."
            : message;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestPaymentSuccess()
    {
        var cart = ReadCart();
        if (cart.Count == 0)
            return Json(new { success = false, message = "Your cart is empty." });

        var address = Request.Form["ShippingAddress"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(address))
            return Json(new { success = false, message = "Delivery address is required." });

        var result = await api.CreateTestSuccessfulPaymentAsync(cart, address);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        Response.Cookies.Delete("cart");
        return Json(new
        {
            success = true,
            redirectUrl = Url.Action(nameof(Success), new { id = result.OrderId })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(PaymentVerifyViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.RazorpayPaymentId) ||
            string.IsNullOrWhiteSpace(model.RazorpaySignature))
            return BadRequest(new { success = false, message = "Payment verification data is incomplete." });

        var result = await api.VerifyRazorpayPaymentAsync(model);
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        Response.Cookies.Delete("cart");
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Success), new { id = model.OrderId }) });
    }

    private List<CartItemViewModel> ReadCart()
    {
        var json = Request.Cookies["cart"];
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? [];
    }
}
