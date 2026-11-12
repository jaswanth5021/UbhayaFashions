using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Customer")]
public class PaymentsController(ApplicationDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, IOrderConfirmationEmailService orderEmail, IDelhiveryService delhivery, ILogger<PaymentsController> logger, IHostEnvironment environment) : ControllerBase
{
    [HttpGet("shipping-quote")]
    public async Task<IActionResult> GetShippingQuote([FromQuery] string destinationPin, [FromQuery] decimal subtotal)
    {
        if (subtotal < 0m) return BadRequest("Invalid subtotal.");

        destinationPin = (destinationPin ?? string.Empty).Trim();
        if (destinationPin.Length != 6 || !destinationPin.All(char.IsDigit))
            return BadRequest("Enter a valid six-digit delivery PIN code.");

        var originPin = configuration["Delhivery:SellerPin"]?.Trim();
        if (string.IsNullOrWhiteSpace(originPin) || originPin.Length != 6 || !originPin.All(char.IsDigit))
            return StatusCode(503, "Delhivery seller PIN is not configured. Please contact support.");

        try
        {
            var serviceability = await delhivery.CheckPincodeServiceabilityAsync(destinationPin, HttpContext.RequestAborted);
            if (!serviceability.Prepaid)
                return BadRequest($"Delhivery does not support prepaid delivery to PIN code {destinationPin}.");

            if (subtotal >= 1000m)
                return Ok(new { subtotal, shippingCharge = 0m, total = subtotal, freeShipping = true });
            var rate = await delhivery.CalculateShippingCostAsync(
                originPin, destinationPin, 1000, "Surface", "Pre-paid", "Delivered", HttpContext.RequestAborted);

            if (rate.ValueKind != JsonValueKind.Array || rate.GetArrayLength() == 0 ||
                !rate[0].TryGetProperty("total_amount", out var amountElement))
                return StatusCode(502, "Delhivery did not return a usable delivery charge.");

            decimal shippingCharge;
            if (amountElement.ValueKind == JsonValueKind.Number && amountElement.TryGetDecimal(out var parsedAmount))
                shippingCharge = parsedAmount;
            else if (amountElement.ValueKind == JsonValueKind.String &&
                     decimal.TryParse(amountElement.GetString(), System.Globalization.NumberStyles.Number,
                         System.Globalization.CultureInfo.InvariantCulture, out parsedAmount))
                shippingCharge = parsedAmount;
            else
                return StatusCode(502, "Delhivery returned an invalid delivery charge.");

            if (shippingCharge <= 0m)
                return StatusCode(503, "Delhivery could not provide a live delivery charge for this PIN code.");

            return Ok(new { subtotal, shippingCharge, total = subtotal + shippingCharge, freeShipping = false });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delhivery delivery quote failed for destination PIN {DestinationPin}", destinationPin);
            return StatusCode(502, "Unable to calculate Delhivery delivery charges right now. Please try again.");
        }
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(CreatePaymentOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        if (!Guid.TryParseExact(request.CheckoutKey, "N", out var checkoutKey))
            return BadRequest("A valid checkout attempt key is required. Refresh checkout and try again.");
        var attemptKey = checkoutKey.ToString("N");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request with { CheckoutKey = null }))));
        await using var checkoutTransaction = await db.Database.BeginTransactionAsync();
        // SQL Server's transaction-owned application lock serializes only retries of this attempt.
        var lockName = $"checkout:{customerId}:{attemptKey}";
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource={lockName}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 50001, 'Checkout is already in progress. Retry shortly.', 1;");
        var existing = await db.Orders.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.CustomerId == customerId && x.CheckoutKey == attemptKey);
        if (existing is not null)
        {
            if (existing.CheckoutFingerprint != fingerprint) return Conflict("This checkout attempt was already used for different details. Refresh checkout.");
            if (existing.Status != "Pending" || existing.PaymentStatus != "Pending" || string.IsNullOrWhiteSpace(existing.RazorpayOrderId))
                return Conflict("This checkout attempt is no longer pending. Check My Orders before starting again.");
            var savedSubtotal = existing.Items.Sum(x => x.Price * x.Quantity);
            return Ok(new { orderId = existing.Id, razorpayOrderId = existing.RazorpayOrderId,
                keyId = configuration["Razorpay:KeyId"], amount = checked((long)Math.Round(existing.TotalAmount * 100m, MidpointRounding.AwayFromZero)),
                subtotal = savedSubtotal, discountAmount = existing.DiscountAmount, couponCode = existing.CouponCode, shippingCharge = existing.TotalAmount - savedSubtotal + existing.DiscountAmount,
                currency = "INR", name = "Ubhaya Fashions", description = $"Order #{existing.Id}" });
        }
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return BadRequest("Delivery address is required.");

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest("Cart is empty.");

        if (request.Items.Any(x => x.Quantity <= 0 || string.IsNullOrWhiteSpace(x.Size)))
            return BadRequest("Choose a size and a valid quantity for each item.");

        var addressError = CheckoutSafety.ValidateAddress(request.DeliveryAddress);
        if (addressError is not null) return BadRequest(addressError);
        if (request.Items.Count > 100 || request.Items.Any(x => x.Quantity > 100))
            return BadRequest("The cart exceeds the allowed quantity.");
        try
        {
            var serviceability = await delhivery.CheckPincodeServiceabilityAsync(request.DeliveryAddress!.PostalCode, HttpContext.RequestAborted);
            if (!serviceability.Prepaid) return BadRequest("Prepaid delivery is unavailable for this PIN code.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Delivery validation failed.");
            return StatusCode(503, "Unable to validate delivery. Please try again.");
        }
        var ids = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products
            .Include(x => x.Variants)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        if (products.Count != ids.Count)
            return BadRequest("One or more products no longer exist.");

        decimal total = 0;
        var order = new Order
        {
            CustomerId = customerId,
            CheckoutKey = attemptKey,
            CheckoutFingerprint = fingerprint,
            ShippingAddress = request.ShippingAddress,
            Status = "Pending",
            PaymentStatus = "Pending",
            PaymentMethod = "Prepaid",
            DeliveryAddressJson = request.DeliveryAddress is null ? null : JsonSerializer.Serialize(request.DeliveryAddress)
        };

        foreach (var item in request.Items)
        {
            var product = products.Single(x => x.Id == item.ProductId);
            var hasColorVariants = product.Variants.Any(x => !string.IsNullOrWhiteSpace(x.Color));
            var variant = hasColorVariants
                ? product.Variants.FirstOrDefault(x => x.Size == item.Size && string.Equals(x.Color, item.Color, StringComparison.OrdinalIgnoreCase))
                : product.Variants.FirstOrDefault(x => x.Size == item.Size);

            if (variant is null)
                return BadRequest($"{product.Name} does not have size {item.Size}.");

            // We intentionally do not reduce stock here. Stock is committed only
            // after the Razorpay payment is verified successfully.
            if (variant.Stock < item.Quantity)
                return BadRequest($"{product.Name} ({variant.Size}) has insufficient stock.");

            total += variant.Price * item.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = variant.Price,
                Quantity = item.Quantity,
                Size = variant.Size,
                Color = variant.Color
            });
        }

        try
        {
            var plan = CheckoutSafety.PlanStock(order.Items, products.SelectMany(x => x.Variants).ToList());
            if (plan.Any(x => x.Quantity > x.Variant.Stock)) return Conflict("The combined quantity exceeds available stock.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or OverflowException)
        { return BadRequest("Invalid size, colour or quantity."); }
        var subtotal = total;
        decimal discountAmount = 0m;
        backend.Models.Coupon? appliedCoupon = null;
        var nowUtc = DateTime.UtcNow;
        var candidates = await db.Coupons.Where(c => c.IsActive &&
            (c.IsAutomatic || (!string.IsNullOrWhiteSpace(request.CouponCode) && c.Code == request.CouponCode.Trim().ToUpper())) &&
            (!c.StartsAtUtc.HasValue || c.StartsAtUtc <= nowUtc) &&
            (!c.EndsAtUtc.HasValue || c.EndsAtUtc >= nowUtc) &&
            (!c.MaximumUses.HasValue || c.UsageCount < c.MaximumUses)).ToListAsync();
        var hasPreviousOrder = await db.Orders.AnyAsync(o => o.CustomerId == customerId &&
            (o.PaymentStatus == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Delivered"));
        var redeemedCouponIds = await db.CouponRedemptions.Where(r => r.CustomerId == customerId).Select(r => r.CouponId).ToListAsync();
        var eligibleCoupons = candidates.Select(c =>
        {
            var eligibleProductIds = products.Where(p =>
                (!c.ProductId.HasValue || p.Id == c.ProductId.Value) &&
                (!c.CategoryId.HasValue || p.CategoryId == c.CategoryId.Value)).Select(p => p.Id).ToHashSet();
            var eligibleSubtotal = order.Items.Where(i => eligibleProductIds.Contains(i.ProductId)).Sum(i => i.Price * i.Quantity);
            var usageAllowed = !c.PerCustomerLimit.HasValue || redeemedCouponIds.Count(id => id == c.Id) < c.PerCustomerLimit.Value;
            var firstOrderAllowed = !c.FirstOrderOnly || !hasPreviousOrder;
            var amount = c.DiscountType == "Percentage" ? eligibleSubtotal * c.DiscountValue / 100m : c.DiscountValue;
            if (c.MaximumDiscount.HasValue) amount = Math.Min(amount, c.MaximumDiscount.Value);
            amount = Math.Round(Math.Clamp(amount, 0m, eligibleSubtotal), 2, MidpointRounding.AwayFromZero);
            return new { Coupon = c, EligibleSubtotal = eligibleSubtotal, UsageAllowed = usageAllowed, FirstOrderAllowed = firstOrderAllowed, Amount = amount };
        }).Where(x => x.EligibleSubtotal >= x.Coupon.MinimumOrderAmount && x.UsageAllowed && x.FirstOrderAllowed && x.Amount > 0m &&
            (x.Coupon.IsAutomatic || string.Equals(x.Coupon.Code, request.CouponCode?.Trim(), StringComparison.OrdinalIgnoreCase)))
          .OrderByDescending(x => x.Amount).ToList();
        if (!string.IsNullOrWhiteSpace(request.CouponCode) && eligibleCoupons.All(x => !string.Equals(x.Coupon.Code, request.CouponCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            return BadRequest("This coupon is invalid or does not apply to your cart.");
        if (eligibleCoupons.Count > 0)
        {
            var selected = eligibleCoupons[0];
            appliedCoupon = selected.Coupon;
            discountAmount = selected.Amount;
            order.CouponId = appliedCoupon.Id;
            order.CouponCode = appliedCoupon.IsAutomatic ? null : appliedCoupon.Code;
            order.DiscountAmount = discountAmount;
        }
        decimal shippingCharge = 0m;
        if (subtotal < 1000m)
        {
            var destinationPin = request.DeliveryAddress?.PostalCode?.Trim();
            var originPin = configuration["Delhivery:SellerPin"]?.Trim();
            if (string.IsNullOrWhiteSpace(destinationPin) || destinationPin.Length != 6 || !destinationPin.All(char.IsDigit))
                return BadRequest("Enter a valid six-digit delivery PIN code to calculate shipping charges.");

            if (string.IsNullOrWhiteSpace(originPin) || originPin.Length != 6 || !originPin.All(char.IsDigit))
                return StatusCode(503, "Delhivery seller PIN is not configured. Please contact support to calculate delivery charges.");

            try
            {
                var serviceability = await delhivery.CheckPincodeServiceabilityAsync(destinationPin, HttpContext.RequestAborted);
                if (!serviceability.Prepaid)
                    return BadRequest($"Delhivery does not support prepaid delivery to PIN code {destinationPin}. Please try another delivery address.");

                // Keep the quote aligned with the 1 kg Surface parcel used when
                // the paid order is manifested in CreatePaidOrderShipmentAsync.
                var rate = await delhivery.CalculateShippingCostAsync(
                    originPin, destinationPin, 1000, "Surface", "Pre-paid", "Delivered", HttpContext.RequestAborted);

                if (rate.ValueKind != JsonValueKind.Array || rate.GetArrayLength() == 0 ||
                    !rate[0].TryGetProperty("total_amount", out var amountElement))
                    return StatusCode(502, "Delhivery did not return a usable delivery charge. Please try again.");

                if (amountElement.ValueKind == JsonValueKind.Number && amountElement.TryGetDecimal(out var parsedAmount))
                    shippingCharge = parsedAmount;
                else if (amountElement.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(amountElement.GetString(), System.Globalization.NumberStyles.Number,
                             System.Globalization.CultureInfo.InvariantCulture, out parsedAmount))
                    shippingCharge = parsedAmount;
                else
                    return StatusCode(502, "Delhivery returned an invalid delivery charge. Please try again.");

                if (shippingCharge <= 0m)
                {
                    var environment = configuration["Delhivery:Environment"] ?? "Staging";
                    var detail = environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                        ? "Delhivery returned a zero delivery charge for this PIN code. Please contact support before payment."
                        : "Delhivery staging does not provide live shipping rates. Set Delhivery:Environment to Production with the live API token to calculate the actual delivery charge.";
                    return StatusCode(503, detail);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delhivery delivery quote failed for destination PIN {DestinationPin}", destinationPin);
                return StatusCode(502, "Unable to calculate Delhivery delivery charges right now. Please try again.");
            }
        }

        total = Math.Max(0m, subtotal - discountAmount) + shippingCharge;
        order.TotalAmount = total;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var keyId = configuration["Razorpay:KeyId"];
        var keySecret = configuration["Razorpay:KeySecret"];

        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret) || keyId.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(500, "Razorpay test API keys are not configured on the backend.");
        }

        var amountInPaise = checked((long)Math.Round(total * 100m, MidpointRounding.AwayFromZero));
        var receipt = $"ubhaya_{order.Id}";

        var client = httpClientFactory.CreateClient("Razorpay");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        using var razorpayResponse = await client.PostAsJsonAsync(
            "orders",
            new
            {
                amount = amountInPaise,
                currency = "INR",
                receipt,
            });

        var responseBody = await razorpayResponse.Content.ReadAsStringAsync();
        if (!razorpayResponse.IsSuccessStatusCode)
        {
            db.Orders.Remove(order);
            await db.SaveChangesAsync();
            logger.LogWarning("Razorpay order creation failed with HTTP {Status}", (int)razorpayResponse.StatusCode);
            return StatusCode(502, "Payment order could not be created. Please try again.");
        }

        using var razorpayJson = JsonDocument.Parse(responseBody);
        var razorpayOrderId = razorpayJson.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(razorpayOrderId))
            return StatusCode(500, "Razorpay did not return an order ID.");

        order.RazorpayOrderId = razorpayOrderId;
        db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            RazorpayOrderId = razorpayOrderId,
            Amount = total,
            Currency = "INR",
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        await checkoutTransaction.CommitAsync();
        return Ok(new
        {
            orderId = order.Id,
            razorpayOrderId,
            keyId,
            amount = amountInPaise,
            subtotal,
            discountAmount,
            couponCode = order.CouponCode,
            shippingCharge,
            currency = "INR",
            name = "Ubhaya Fashions",
            description = $"Order #{order.Id}"
        });
    }


    // TEST ONLY: simulates a successful payment without contacting Razorpay.
    // Keep PaymentTesting:Enabled=false (or unset) outside the feature/test environment.
    [HttpPost("test-success")]
    public async Task<IActionResult> TestSuccess(CreatePaymentOrderRequest request)
    {
        if (!CheckoutSafety.TestPaymentsAllowed(environment, configuration))
            return NotFound();

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ShippingAddress) ||
            request.Items is null || request.Items.Count == 0)
            return BadRequest("Shipping address and cart items are required.");

        if (request.Items.Any(x => x.Quantity <= 0 || string.IsNullOrWhiteSpace(x.Size)))
            return BadRequest("Choose a size and a valid quantity for each item.");

        var ids = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products
            .Include(x => x.Variants)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        if (products.Count != ids.Count)
            return BadRequest("One or more products no longer exist.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            decimal total = 0;
            var order = new Order
            {
                CustomerId = customerId,
                ShippingAddress = request.ShippingAddress,
                Status = "Processing",
                PaymentStatus = "Paid",
                InventoryCommitted = true,
                PaymentMethod = "Prepaid"
            };

            foreach (var item in request.Items)
            {
                var product = products.Single(x => x.Id == item.ProductId);
                var variant = product.Variants.FirstOrDefault(x => x.Size == item.Size);

                if (variant is null)
                    return BadRequest($"{product.Name} does not have size {item.Size}.");

                if (variant.Stock < item.Quantity)
                    return BadRequest($"{product.Name} ({variant.Size}) has insufficient stock.");

                total += variant.Price * item.Quantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = variant.Price,
                    Quantity = item.Quantity,
                    Size = variant.Size,
                    Color = item.Color
                });

                variant.Stock -= item.Quantity;
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    Size = variant.Size,
                    Type = "Sale",
                    Quantity = -item.Quantity,
                    StockAfter = variant.Stock,
                    Note = "TEST payment for order"
                });
            }

            order.TotalAmount = total;
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            await db.Entry(order).Reference(x => x.Customer).LoadAsync();
            await orderEmail.SendOrderConfirmationAsync(order, order.Customer);

            return Ok(new { success = true, orderId = order.Id, status = "Paid" });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyPaymentRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Include(x => x.Customer)
            .Include(x => x.DelhiveryShipment)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId && x.CustomerId == customerId);

        if (order is null)
            return NotFound("Order not found.");

        var payment = order.Payments.FirstOrDefault(x => x.RazorpayOrderId == request.RazorpayOrderId);
        if (payment is null)
            return BadRequest("Payment record not found.");

        if (order.RazorpayOrderId != request.RazorpayOrderId)
            return BadRequest("Razorpay order mismatch.");

        // Razorpay requires HMAC-SHA256(order_id + "|" + payment_id, secret).
        // Use the server-trusted Razorpay order ID stored in our DB.
        var secret = configuration["Razorpay:KeySecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(500, "Razorpay secret is not configured.");

        var payload = $"{order.RazorpayOrderId}|{request.RazorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.RazorpaySignature) || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(request.RazorpaySignature)))
            return BadRequest("Invalid Razorpay payment signature.");

        // Verify the payment with Razorpay before changing our order state.
        var keyId = configuration["Razorpay:KeyId"];
        var keySecret = configuration["Razorpay:KeySecret"];
        var client = httpClientFactory.CreateClient("Razorpay");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        using var paymentResponse = await client.GetAsync($"payments/{Uri.EscapeDataString(request.RazorpayPaymentId)}");
        var paymentBody = await paymentResponse.Content.ReadAsStringAsync();
        if (!paymentResponse.IsSuccessStatusCode)
            return BadRequest("Unable to verify the Razorpay payment status.");

        using var paymentJson = JsonDocument.Parse(paymentBody);
        var root = paymentJson.RootElement;
        var razorpayStatus = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
        var razorpayOrderId = root.TryGetProperty("order_id", out var orderElement) ? orderElement.GetString() : null;
        var razorpayAmount = root.TryGetProperty("amount", out var amountElement) ? amountElement.GetInt64() : -1;
        var expectedAmount = checked((long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero));

        if (!string.Equals(razorpayStatus, "captured", StringComparison.OrdinalIgnoreCase))
            return BadRequest($"Payment is not captured. Razorpay status: {razorpayStatus ?? "unknown"}.");

        var currency = root.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString() : null;
        if (!string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(razorpayOrderId, order.RazorpayOrderId, StringComparison.Ordinal) || razorpayAmount != expectedAmount)
            return BadRequest("Razorpay payment amount or order does not match our order.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var lockedOrder = await CheckoutSafety.LockOrderAsync(db, order.Id);
        if (lockedOrder is null || lockedOrder.CustomerId != customerId) return NotFound();
        var lockedPayment = lockedOrder.Payments.Single(x => x.Id == payment.Id);
        if (lockedPayment.RazorpayPaymentId is not null && lockedPayment.RazorpayPaymentId != request.RazorpayPaymentId)
            return Conflict("A different captured payment is already recorded for this order.");
        await CheckoutSafety.ApplyCaptureAsync(db, lockedOrder, lockedPayment, request.RazorpayPaymentId);
        lockedPayment.Signature = request.RazorpaySignature;
        if (lockedOrder.PaymentStatus == "Paid" && lockedOrder.CouponId.HasValue &&
            !await db.CouponRedemptions.AnyAsync(x => x.OrderId == lockedOrder.Id))
        {
            var redeemedCoupon = await db.Coupons.SingleOrDefaultAsync(x => x.Id == lockedOrder.CouponId.Value);
            if (redeemedCoupon is not null)
            {
                redeemedCoupon.UsageCount++;
                db.CouponRedemptions.Add(new CouponRedemption
                {
                    CouponId = redeemedCoupon.Id,
                    CustomerId = lockedOrder.CustomerId,
                    OrderId = lockedOrder.Id,
                    RedeemedAtUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (lockedOrder.PaymentStatus != "Paid" || CheckoutSafety.IsCancelled(lockedOrder))
            return Conflict("Payment received; the order requires refund or stock review. Please contact support.");
        return Ok(new { success = true, orderId = lockedOrder.Id, status = "Paid", shipmentStatus = "Pending" });
    }
    internal static async Task CreatePaidOrderShipmentAsync(
        Order order,
        PaymentDeliveryAddress? address,
        IDelhiveryService delhivery,
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger)
    {
        if (order.DelhiveryShipment is not null && !string.IsNullOrWhiteSpace(order.DelhiveryShipment.Waybill))
            return;

        var pickup = configuration["Delhivery:PickupLocationName"];
        var clientName = configuration["Delhivery:ClientName"];
        if (address is null ||
            string.IsNullOrWhiteSpace(address.Name) ||
            string.IsNullOrWhiteSpace(address.Mobile) ||
            string.IsNullOrWhiteSpace(address.AddressLine1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) ||
            string.IsNullOrWhiteSpace(address.PostalCode) ||
            string.IsNullOrWhiteSpace(pickup) ||
            string.IsNullOrWhiteSpace(clientName))
        {
            logger.LogWarning("Order {OrderId} is paid, but automatic Delhivery shipment was skipped because delivery address or Delhivery configuration is incomplete.", order.Id);
            return;
        }

        var shipment = order.DelhiveryShipment ?? new DelhiveryShipment { OrderId = order.Id };
        if (order.DelhiveryShipment is null)
            db.DelhiveryShipments.Add(shipment);

        shipment.PickupLocation = pickup.Trim();
        shipment.RecipientName = address.Name.Trim();
        shipment.RecipientPhone = address.Mobile.Trim();
        shipment.Address = string.Join(", ", new[] { address.AddressLine1, address.AddressLine2 }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        shipment.City = address.City.Trim();
        shipment.State = address.State.Trim();
        shipment.PostalCode = address.PostalCode.Trim();
        shipment.Country = string.IsNullOrWhiteSpace(address.Country) ? "India" : address.Country.Trim();
        shipment.WeightGrams = 1000;
        shipment.ShipmentStatus = "Creating";
        shipment.LastError = null;
        shipment.AttemptCount++;
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        try
        {
            var created = await delhivery.CreateShipmentAsync(order, shipment, CancellationToken.None);
            shipment.Waybill = created.Waybill;
            shipment.LabelUrl = created.LabelUrl;
            shipment.ShipmentStatus = "Shipment Created";
            shipment.LastError = null;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogInformation("Automatic Delhivery shipment created for paid order {OrderId}; AWB {Waybill}.", order.Id, shipment.Waybill);
        }
        catch (Exception ex)
        {
            shipment.ShipmentStatus = "Creation Failed";
            shipment.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogError(ex, "Order {OrderId} is paid, but automatic Delhivery shipment creation failed. Admin retry is required.", order.Id);
        }
    }

}
