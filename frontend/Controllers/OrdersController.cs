using System.Text.Json;
using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

[Authorize]
public class OrdersController(ApiService api, ILogger<OrdersController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Profile", "Account", new { tab = "orders" });

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

        result.Data.SaveAddress = model.SaveAddress;
        result.Data.SelectedSavedAddressId = model.SelectedSavedAddressId;
        result.Data.Address = model.Address;
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
    public async Task<IActionResult> TestPaymentSuccess(CreateOrderViewModel model)
    {
        var cart = ReadCart();
        if (cart.Count == 0)
            return Json(new { success = false, message = "Your cart is empty." });

        var address = model.ShippingAddress.Trim();
        if (string.IsNullOrWhiteSpace(address))
            return Json(new { success = false, message = "Delivery address is required." });

        var result = await api.CreateTestSuccessfulPaymentAsync(cart, address);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        await SaveNewAddressAfterPaymentAsync(model.SaveAddress, model.SelectedSavedAddressId, model.Address);
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

        await SaveNewAddressAfterPaymentAsync(model.SaveAddress, model.SelectedSavedAddressId, model.Address);
        Response.Cookies.Delete("cart");
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Success), new { id = model.OrderId }) });
    }

    private async Task SaveNewAddressAfterPaymentAsync(bool saveAddress, int selectedAddressId, SaveAddressViewModel address)
    {
        if (!saveAddress || selectedAddressId > 0)
            return;

        if (string.IsNullOrWhiteSpace(address.Name) ||
            string.IsNullOrWhiteSpace(address.Mobile) ||
            string.IsNullOrWhiteSpace(address.AddressLine1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) ||
            string.IsNullOrWhiteSpace(address.PostalCode))
        {
            logger.LogWarning("Address was not saved after successful payment because required address fields were missing.");
            return;
        }

        address.Id = 0;
        address.IsDefault = false;

        try
        {
            var savedAddresses = await api.GetMyAddressesAsync();
            if (savedAddresses.Any(saved =>
                    string.Equals(saved.Name.Trim(), address.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.Mobile.Trim(), address.Mobile.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.AddressLine1.Trim(), address.AddressLine1.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.AddressLine2?.Trim() ?? "", address.AddressLine2?.Trim() ?? "", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.City.Trim(), address.City.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.State.Trim(), address.State.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.PostalCode.Trim(), address.PostalCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(saved.Country.Trim(), address.Country.Trim(), StringComparison.OrdinalIgnoreCase)))
                return;

            var result = await api.SaveAddressAsync(address);
            if (!result.Success)
                logger.LogWarning("Order succeeded, but saving the checkout address failed: {Message}", result.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order succeeded, but saving the checkout address failed.");
        }
    }

    private List<CartItemViewModel> ReadCart()
    {
        var json = Request.Cookies["cart"];
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? [];
    }
}
