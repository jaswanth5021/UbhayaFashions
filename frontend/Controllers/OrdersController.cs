using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0) return NotFound();

        try
        {
            // Read from the authenticated customer's order list so an order ID
            // cannot be used to view another customer's order.
            var json = await api.GetMyOrdersAsync();
            var orders = JsonSerializer.Deserialize<List<MyOrderViewModel>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var order = orders.FirstOrDefault(item => item.Id == id);
            if (order is null) return NotFound();

            return View(order);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Unable to load details for order {OrderId}", id);
            TempData["Error"] = "We couldn't load your order details. Please try again.";
            return RedirectToAction("Profile", "Account", new { tab = "orders" });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unable to parse order details for order {OrderId}", id);
            TempData["Error"] = "Your order details are temporarily unavailable.";
            return RedirectToAction("Profile", "Account", new { tab = "orders" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (id <= 0) return NotFound();

        try
        {
            var result = await api.CancelMyOrderAsync(id);
            if (!result.Success)
            {
                var message = result.Message;
                try
                {
                    using var document = JsonDocument.Parse(result.Message);
                    if (document.RootElement.TryGetProperty("message", out var apiMessage))
                        message = apiMessage.GetString() ?? message;
                }
                catch (JsonException) { }

                TempData["ProfileError"] = string.IsNullOrWhiteSpace(message)
                    ? "We couldn't cancel this order. Please try again."
                    : message.Trim('"');
            }
            else
            {
                TempData["ProfileSuccess"] = "Your order has been cancelled. If payment was captured, the refund is pending processing.";
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Unable to cancel order {OrderId}", id);
            TempData["ProfileError"] = "We couldn't reach the order service. Please try again.";
        }

        return RedirectToAction("Profile", "Account", new { tab = "orders" });
    }

    [HttpGet]
    public async Task<IActionResult> Track(int id)
    {
        if (id <= 0) return NotFound();
        try
        {
            var tracking = await api.GetOrderTrackingAsync(id);
            if (tracking is null) return NotFound();
            var orderJson = await api.GetMyOrdersAsync();
            var customerOrders = JsonSerializer.Deserialize<List<MyOrderViewModel>>(orderJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            var firstItem = customerOrders.FirstOrDefault(order => order.Id == id)?.Items.FirstOrDefault();
            if (firstItem is not null)
            {
                tracking.FirstItemProductId = firstItem.ProductId;
                tracking.FirstItemName ??= firstItem.ProductName;
                tracking.FirstItemImageUrl ??= firstItem.ImageUrl;
            }
            return View("Track", tracking);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Unable to load tracking for order {OrderId}", id);
            TempData["Error"] = "We couldn't load tracking details. Please try again.";
            return RedirectToAction("Profile", "Account", new { tab = "orders" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Find(string? orderId, int current)
    {
        var digits = new string((orderId ?? "").Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var id) || id <= 0)
        {
            TempData["Error"] = "Enter a valid order ID.";
            return current > 0 ? RedirectToAction(nameof(Track), new { id = current }) : RedirectToAction(nameof(Index));
        }

        try
        {
            if (await api.GetOrderTrackingAsync(id) is not null)
                return RedirectToAction(nameof(Track), new { id });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Unable to look up order {OrderId}", id);
        }

        TempData["Error"] = $"We couldn't find order #{id} in your account.";
        return current > 0 ? RedirectToAction(nameof(Track), new { id = current }) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeliveryQuote(string pincode, decimal subtotal)
    {
        if (subtotal < 0m)
            return BadRequest(new { message = "Invalid order subtotal." });

        if (subtotal >= 1000m)
            return Json(new { success = true, subtotal, shippingCharge = 0m, total = subtotal, freeShipping = true });

        if (string.IsNullOrWhiteSpace(pincode) || pincode.Trim().Length != 6 || !pincode.Trim().All(char.IsDigit))
            return BadRequest(new { success = false, message = "Enter a valid six-digit delivery PIN code in the Address step." });

        var quote = await api.GetDelhiveryShippingQuoteAsync(pincode.Trim(), subtotal);
        if (!quote.Success)
            return StatusCode(502, new { success = false, message = quote.Message.Trim().Trim('"') });

        return Json(new { success = true, subtotal, shippingCharge = quote.ShippingCharge, total = subtotal + quote.ShippingCharge, freeShipping = false });
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

        var cartVariants = new Dictionary<int, List<ProductVariantViewModel>>();
        foreach (var productId in cart.Select(item => item.ProductId).Distinct())
        {
            try
            {
                var product = await api.GetProductAsync(productId);
                if (product is not null)
                    cartVariants[productId] = product.Variants;
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Unable to load stock variants for checkout product {ProductId}", productId);
                cartVariants[productId] = [];
            }
        }

        ViewBag.SavedAddresses = savedAddresses;
        ViewBag.CartVariants = cartVariants;
        // Use the same public product-offers API as the product detail page.
        // The checkout-offers endpoint requires a customer JWT and was returning
        // an empty list when the server-side storefront request had no usable token.
        var checkoutOffers = new Dictionary<int, CheckoutCouponOfferViewModel>();
        foreach (var productId in cart.Select(item => item.ProductId).Distinct())
        {
            try
            {
                var productOffers = await api.GetProductCouponOffersAsync(productId);
                foreach (var offer in productOffers)
                {
                    if (!checkoutOffers.ContainsKey(offer.Id))
                    {
                        var eligibleSubtotal = cart
                            .Where(item => (!offer.ProductId.HasValue || item.ProductId == offer.ProductId.Value))
                            .Sum(item => item.Price * item.Quantity);
                        // A product-specific coupon must not be used with a mixed-product cart.
                        var cartMatchesProductScope = !offer.ProductId.HasValue ||
                            cart.All(item => item.ProductId == offer.ProductId.Value);
                        // Category-scoped offers are already returned only for products
                        // in the matching category by the product-offers endpoint.
                        // Some deployed API instances may still return only DiscountText
                        // (for example, "10% OFF") without the numeric calculation fields.
                        // Fall back to the display text so valid coupons are not rejected
                        // with a zero discount while the API deployment is being aligned.
                        var discountType = offer.DiscountType;
                        var discountValue = offer.DiscountValue;
                        var maximumDiscount = offer.MaximumDiscount;
                        if (discountValue <= 0m)
                        {
                            var percentageMatch = Regex.Match(offer.DiscountText ?? string.Empty,
                                @"(?<value>\d+(?:\.\d+)?)\s*%\s*OFF", RegexOptions.IgnoreCase);
                            var fixedMatch = Regex.Match(offer.DiscountText ?? string.Empty,
                                @"₹\s*(?<value>[\d,]+(?:\.\d+)?)\s*OFF", RegexOptions.IgnoreCase);
                            var capMatch = Regex.Match(offer.DiscountText ?? string.Empty,
                                @"up to\s*₹?\s*(?<value>[\d,]+(?:\.\d+)?)", RegexOptions.IgnoreCase);

                            if (percentageMatch.Success &&
                                decimal.TryParse(percentageMatch.Groups["value"].Value, NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out var parsedPercentage))
                            {
                                discountType = "Percentage";
                                discountValue = parsedPercentage;
                            }
                            else if (fixedMatch.Success &&
                                decimal.TryParse(fixedMatch.Groups["value"].Value.Replace(",", string.Empty),
                                    NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedFixed))
                            {
                                discountType = "Fixed";
                                discountValue = parsedFixed;
                            }

                            if (!maximumDiscount.HasValue && capMatch.Success &&
                                decimal.TryParse(capMatch.Groups["value"].Value.Replace(",", string.Empty),
                                    NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCap))
                            {
                                maximumDiscount = parsedCap;
                            }
                        }

                        var calculatedDiscount = string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase)
                            ? eligibleSubtotal * discountValue / 100m
                            : discountValue;
                        if (maximumDiscount.HasValue)
                            calculatedDiscount = Math.Min(calculatedDiscount, maximumDiscount.Value);
                        calculatedDiscount = Math.Round(Math.Clamp(calculatedDiscount, 0m, eligibleSubtotal), 2, MidpointRounding.AwayFromZero);
                        checkoutOffers[offer.Id] = new CheckoutCouponOfferViewModel
                        {
                            Id = offer.Id,
                            Code = offer.Code,
                            Name = offer.Name,
                            DiscountText = offer.DiscountText,
                            DiscountAmount = calculatedDiscount,
                            MinimumOrderAmount = offer.MinimumOrderAmount,
                            IsAutomatic = offer.IsAutomatic,
                            FirstOrderOnly = offer.FirstOrderOnly,
                            ProductId = offer.ProductId,
                            CategoryId = offer.CategoryId,
                            AppliesTo = offer.AppliesTo,
                            EndsAtUtc = offer.EndsAtUtc,
                            Terms = offer.Terms,
                            CanApply = cartMatchesProductScope && eligibleSubtotal >= offer.MinimumOrderAmount && calculatedDiscount > 0m,
                            EligibilityMessage = !cartMatchesProductScope
                                ? $"This coupon applies only to {cart.FirstOrDefault(item => item.ProductId == offer.ProductId)?.Name ?? "the eligible product"}. Remove other products from your cart to use this coupon."
                                : eligibleSubtotal < offer.MinimumOrderAmount
                                    ? $"Minimum order ₹{offer.MinimumOrderAmount:0.##} is required for eligible items."
                                    : calculatedDiscount <= 0m
                                        ? "This offer does not add a discount to your current cart."
                                        : string.Empty
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Unable to load product coupon offers for checkout product {ProductId}", productId);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Unable to read product coupon offers for checkout product {ProductId}", productId);
            }
        }

        ViewBag.CheckoutCouponOffers = checkoutOffers.Values
            .OrderByDescending(offer => offer.CanApply)
            .ThenBy(offer => offer.Name)
            .Take(8)
            .ToList();
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

        foreach (var item in cart)
        {
            var product = await api.GetProductAsync(item.ProductId);
            var hasColorVariants = product?.Variants.Any(variant => !string.IsNullOrWhiteSpace(variant.Color)) == true;
            var sizeVariants = product?.Variants
                .Where(variant => string.Equals(variant.Size, item.Size, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? [];

            var availableVariant = hasColorVariants
                ? sizeVariants.FirstOrDefault(variant =>
                    !string.IsNullOrWhiteSpace(item.Color) &&
                    string.Equals(variant.Color, item.Color, StringComparison.OrdinalIgnoreCase))
                    ?? sizeVariants.FirstOrDefault(variant => string.IsNullOrWhiteSpace(variant.Color))
                : sizeVariants.FirstOrDefault();

            if (availableVariant is null || availableVariant.Stock < 1)
            {
                TempData["Error"] = $"{item.Name} ({item.Size}) is no longer available. Please choose another size or remove it.";
                return RedirectToAction(nameof(Checkout));
            }

            if (item.Quantity > availableVariant.Stock)
            {
                TempData["Error"] = $"Only {availableVariant.Stock} item(s) of {item.Name} in size {item.Size} are available. Please update the quantity.";
                return RedirectToAction(nameof(Checkout));
            }

            // Refresh the cart price from the current variant before creating the payment order.
            item.Price = availableVariant.Price;
            item.DiscountPercent = availableVariant.Discount;
        }

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

        var result = await api.CreateRazorpayOrderAsync(cart, model.ShippingAddress, model.Address, model.CheckoutKey, model.CouponCode);
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
    public async Task<IActionResult> Success(int id)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));

        ViewBag.OrderId = id;
        try
        {
            // The tracking endpoint scopes the lookup to the signed-in customer.
            var tracking = await api.GetOrderTrackingAsync(id);
            if (tracking is null) return NotFound();

            ViewBag.Waybill = tracking.Waybill;
            ViewBag.ShippingStatus = tracking.ShippingStatus;
            ViewBag.DelhiTrackingUrl = string.IsNullOrWhiteSpace(tracking.Waybill)
                ? null
                : $"https://www.delhivery.com/track/package/{Uri.EscapeDataString(tracking.Waybill)}";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Could not load shipment details for success page, order {OrderId}.", id);
            ViewBag.ShippingStatus = "Tracking details are being updated";
        }

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
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()) return NotFound();
        var cart = ReadCart();
        if (cart.Count == 0)
            return Json(new { success = false, message = "Your cart is empty." });

        var address = model.ShippingAddress.Trim();
        if (string.IsNullOrWhiteSpace(address))
            return Json(new { success = false, message = "Delivery address is required." });

        var result = await api.CreateTestSuccessfulPaymentAsync(cart, address, model.CouponCode);
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
