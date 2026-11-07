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
[Authorize]
public class PaymentsController(ApplicationDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, IOrderConfirmationEmailService orderEmail, IDelhiveryService delhivery, IGa4MeasurementProtocolService ga4, ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(CreatePaymentOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return BadRequest("Delivery address is required.");

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest("Cart is empty.");

        if (request.Items.Any(x => x.Quantity <= 0 || string.IsNullOrWhiteSpace(x.Size)))
            return BadRequest("Choose a size and a valid quantity for each item.");

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
                Color = item.Color
            });
        }

        var subtotal = total;
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

        total += shippingCharge;
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
            return StatusCode((int)razorpayResponse.StatusCode, "Razorpay order creation failed: " + responseBody);
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

        return Ok(new
        {
            orderId = order.Id,
            razorpayOrderId,
            keyId,
            amount = amountInPaise,
            subtotal,
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
        if (!configuration.GetValue<bool>("PaymentTesting:Enabled"))
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

        var order = await db.Orders
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

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(request.RazorpaySignature)))
            return BadRequest("Invalid Razorpay payment signature.");

        // Idempotent retry: do not reduce stock twice.
        if (order.PaymentStatus == "Paid" || payment.Status == "Captured")
        {
            await ga4.TrackPurchaseAsync(payment.Id);
            return Ok(new { success = true, orderId = order.Id, status = "Paid" });
        }

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

        if (!string.Equals(razorpayOrderId, order.RazorpayOrderId, StringComparison.Ordinal) || razorpayAmount != expectedAmount)
            return BadRequest("Razorpay payment amount or order does not match our order.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Serialize payment finalization for this payment row. If the webhook
            // committed first, this lock/read observes Captured and prevents a second stock deduction.
            var currentPayment = await db.Payments
                .FromSqlInterpolated($"SELECT * FROM dbo.Payments WITH (UPDLOCK, ROWLOCK) WHERE Id = {payment.Id}")
                .AsNoTracking()
                .SingleAsync();

            if (currentPayment.Status == "Captured")
            {
                await transaction.CommitAsync();
                await ga4.TrackPurchaseAsync(payment.Id);
                return Ok(new { success = true, orderId = order.Id, status = "Paid" });
            }

            // Re-check stock inside the transaction immediately before committing it.
            var productIds = order.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await db.Products
                .Include(x => x.Variants)
                .Where(x => productIds.Contains(x.Id))
                .ToListAsync();

            foreach (var item in order.Items)
            {
                var product = products.Single(x => x.Id == item.ProductId);
                var variant = product.Variants.FirstOrDefault(x => x.Size == item.Size);
                if (variant is null || variant.Stock < item.Quantity)
                {
                    // The payment has already been captured. In this rare race condition,
                    // leave the payment/order pending for manual refund handling rather than
                    // silently marking an order as paid without inventory.
                    return Conflict($"Payment captured, but stock is no longer available for {item.ProductName} ({item.Size}). Please contact support for a refund.");
                }

                variant.Stock -= item.Quantity;
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    Size = variant.Size,
                    Type = "Sale",
                    Quantity = -item.Quantity,
                    StockAfter = variant.Stock,
                    Note = $"Razorpay payment for order #{order.Id}"
                });
            }

            order.PaymentStatus = "Paid";
            order.Status = "Processing";
            payment.Status = "Captured";
            payment.RazorpayPaymentId = request.RazorpayPaymentId;
            payment.Signature = request.RazorpaySignature;
            payment.PaidDate = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            // GA4 tracking is best-effort and must never undo a verified payment.
            await ga4.TrackPurchaseAsync(payment.Id);

            // Payment is already committed. Courier failures must not reverse a successful payment.
            try
            {
                await CreatePaidOrderShipmentAsync(order, request.DeliveryAddress, delhivery, db, configuration, logger);
            }
            catch (Exception shipmentException)
            {
                logger.LogError(shipmentException, "Order {OrderId} is paid, but automatic shipment processing could not complete. Admin retry is required.", order.Id);
            }

            await orderEmail.SendOrderConfirmationAsync(order, order.Customer);

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                status = "Paid",
                waybill = order.DelhiveryShipment?.Waybill,
                shipmentStatus = order.DelhiveryShipment?.ShipmentStatus ?? "Creation Failed"
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
