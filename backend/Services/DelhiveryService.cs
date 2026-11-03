using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using backend.Models;

namespace backend.Services;

public sealed record DelhiveryPincodeResult(
    string PostalCode,
    bool IsServiceable,
    bool Prepaid,
    bool Cod,
    bool Pickup,
    string RawResponse);

public sealed record DelhiveryLabelResult(byte[] Content, string ContentType, string FileName);

public interface IDelhiveryService
{
    Task<DelhiveryPincodeResult> CheckPincodeServiceabilityAsync(string postalCode, CancellationToken cancellationToken);
    Task<JsonElement> CalculateShippingCostAsync(string originPin, string destinationPin, int weightGrams, string shipmentMode, string paymentMode, string status, CancellationToken cancellationToken);
    Task<string> FetchWaybillsAsync(int count, CancellationToken cancellationToken);
    Task<string> CreatePickupRequestAsync(string pickupDate, string pickupTime, int expectedPackageCount, CancellationToken cancellationToken);
    Task<DelhiveryLabelResult> DownloadLabelAsync(string waybill, CancellationToken cancellationToken);
    Task<(string Waybill, string? LabelUrl)> CreateShipmentAsync(Order order, DelhiveryShipment shipment, CancellationToken cancellationToken);
    Task<string?> RefreshTrackingAsync(string waybill, CancellationToken cancellationToken);
}

public sealed class DelhiveryService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<DelhiveryService> logger) : IDelhiveryService
{
    private HttpClient CreateClient()
    {
        var configuredToken = configuration["Delhivery:ApiToken"];
        var token = configuredToken?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Delhivery:ApiToken is not configured. Check appsettings.Development.json, User Secrets, and environment variables.");

        var client = httpClientFactory.CreateClient("Delhivery");
        logger.LogInformation(
            "Delhivery client configured. TokenPresent={TokenPresent}, TokenLength={TokenLength}, BaseAddress={BaseAddress}",
            true,
            token.Length,
            client.BaseAddress?.GetLeftPart(UriPartial.Authority) ?? "NotConfigured");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task<DelhiveryPincodeResult> CheckPincodeServiceabilityAsync(
        string postalCode, CancellationToken cancellationToken)
    {
        ValidatePincode(postalCode, nameof(postalCode));
        using var response = await CreateClient().GetAsync(
            $"c/api/pin-codes/json/?filter_codes={Uri.EscapeDataString(postalCode)}",
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "Pincode serviceability");

        using var document = JsonDocument.Parse(body);
        var deliveryCodes = document.RootElement.TryGetProperty("delivery_codes", out var codes) &&
                            codes.ValueKind == JsonValueKind.Array
            ? codes
            : default;

        if (deliveryCodes.ValueKind != JsonValueKind.Array || deliveryCodes.GetArrayLength() == 0)
            return new DelhiveryPincodeResult(postalCode, false, false, false, false, body);

        foreach (var item in deliveryCodes.EnumerateArray())
        {
            if (!item.TryGetProperty("postal_code", out var postal) || postal.ValueKind != JsonValueKind.Object)
                continue;

            var returnedPin = GetString(postal, "pin");
            if (!string.IsNullOrWhiteSpace(returnedPin) &&
                !string.Equals(returnedPin, postalCode, StringComparison.OrdinalIgnoreCase))
                continue;

            var prepaid = IsYes(GetString(postal, "pre_paid"));
            var cod = IsYes(GetString(postal, "cash"));
            var pickup = IsYes(GetString(postal, "pickup"));
            return new DelhiveryPincodeResult(postalCode, prepaid || cod, prepaid, cod, pickup, body);
        }

        return new DelhiveryPincodeResult(postalCode, false, false, false, false, body);
    }

    public async Task<JsonElement> CalculateShippingCostAsync(
        string originPin, string destinationPin, int weightGrams, string shipmentMode,
        string paymentMode, string status, CancellationToken cancellationToken)
    {
        ValidatePincode(originPin, nameof(originPin));
        ValidatePincode(destinationPin, nameof(destinationPin));
        if (weightGrams < 1 || weightGrams > 100000)
            throw new ArgumentOutOfRangeException(nameof(weightGrams), "Weight must be between 1 and 100000 grams.");

        var billingMode = shipmentMode.Trim().Equals("Express", StringComparison.OrdinalIgnoreCase) ||
                          shipmentMode.Trim().Equals("E", StringComparison.OrdinalIgnoreCase) ? "E" : "S";
        var normalizedPayment = paymentMode.Equals("COD", StringComparison.OrdinalIgnoreCase) ? "COD" : "Pre-paid";
        var normalizedStatus = status.Trim();
        if (normalizedStatus is not ("Delivered" or "RTO" or "DTO"))
            throw new ArgumentException("Status must be Delivered, RTO or DTO.", nameof(status));

        var query = $"md={billingMode}&ss={Uri.EscapeDataString(normalizedStatus)}" +
                    $"&d_pin={Uri.EscapeDataString(destinationPin)}&o_pin={Uri.EscapeDataString(originPin)}" +
                    $"&cgm={weightGrams.ToString(CultureInfo.InvariantCulture)}&pt={Uri.EscapeDataString(normalizedPayment)}";

        using var response = await CreateClient().GetAsync($"api/kinko/v1/invoice/charges/.json?{query}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "Shipping cost calculation");
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    public async Task<string> FetchWaybillsAsync(int count, CancellationToken cancellationToken)
    {
        if (count < 1 || count > 10000)
            throw new ArgumentOutOfRangeException(nameof(count), "Waybill count must be between 1 and 10000.");

        using var response = await CreateClient().GetAsync(
            $"waybill/api/bulk/?count={count.ToString(CultureInfo.InvariantCulture)}",
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "Waybill fetch");
        return body;
    }

    public async Task<string> CreatePickupRequestAsync(
        string pickupDate, string pickupTime, int expectedPackageCount, CancellationToken cancellationToken)
    {
        var pickupLocation = configuration["Delhivery:PickupLocationName"];
        if (string.IsNullOrWhiteSpace(pickupLocation))
            throw new InvalidOperationException("Delhivery:PickupLocationName is not configured.");

        if (!DateOnly.TryParseExact(pickupDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate) ||
            parsedDate < DateOnly.FromDateTime(DateTime.Today) ||
            parsedDate > DateOnly.FromDateTime(DateTime.Today.AddDays(7)))
            throw new ArgumentException("Pickup date must be today or within the next 7 days (yyyy-MM-dd).", nameof(pickupDate));

        if (!TimeOnly.TryParseExact(pickupTime, "HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ArgumentException("Pickup time must use HH:mm:ss format.", nameof(pickupTime));

        if (expectedPackageCount < 1 || expectedPackageCount > 1000)
            throw new ArgumentOutOfRangeException(nameof(expectedPackageCount), "Expected package count must be between 1 and 1000.");

        var payload = new
        {
            pickup_time = pickupTime,
            pickup_date = pickupDate,
            pickup_location = pickupLocation,
            expected_package_count = expectedPackageCount
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "fm/request/new/")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await CreateClient().SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "Pickup request creation");
        return body;
    }

    public async Task<DelhiveryLabelResult> DownloadLabelAsync(string waybill, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(waybill))
            throw new ArgumentException("A manifested Delhivery waybill is required.", nameof(waybill));

        var baseUrl = configuration["Delhivery:LabelBaseUrl"];
        using var client = CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/p/packing_slip?wbns={Uri.EscapeDataString(waybill.Trim())}&pdf=True");

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            var labelUri = new Uri(new Uri(baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/"),
                $"api/p/packing_slip?wbns={Uri.EscapeDataString(waybill.Trim())}&pdf=True");
            request.RequestUri = labelUri;
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Delhivery label request failed ({(int)response.StatusCode}): {SafeBody(bytes)}");

        if (!contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) &&
            !(bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46))
            throw new InvalidOperationException($"Delhivery did not return a PDF label. Response: {SafeBody(bytes)}");

        return new DelhiveryLabelResult(bytes, "application/pdf", $"Delhivery-{waybill.Trim()}.pdf");
    }

    public async Task<(string Waybill, string? LabelUrl)> CreateShipmentAsync(
        Order order, DelhiveryShipment shipment, CancellationToken cancellationToken)
    {
        var pickup = configuration["Delhivery:PickupLocationName"];
        if (string.IsNullOrWhiteSpace(pickup))
            throw new InvalidOperationException("Delhivery:PickupLocationName is not configured.");

        var clientName = configuration["Delhivery:ClientName"];
        if (string.IsNullOrWhiteSpace(clientName))
            throw new InvalidOperationException("Delhivery:ClientName is not configured. Set it to the exact client name shown in your Delhivery B2C account.");

        ValidatePincode(shipment.PostalCode, nameof(shipment.PostalCode));
        if (shipment.RecipientPhone.Count(char.IsDigit) < 10)
            throw new InvalidOperationException("A valid recipient phone number is required.");

        var paymentMode = string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase)
            ? "COD"
            : "Pre-paid";
        if (paymentMode == "Pre-paid" &&
            !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prepaid shipment creation is allowed only after payment is verified.");

        var payload = new
        {
            shipments = new[]
            {
                new
                {
                    client = clientName,
                    name = shipment.RecipientName,
                    add = shipment.Address,
                    pin = shipment.PostalCode,
                    city = shipment.City,
                    state = shipment.State,
                    country = shipment.Country,
                    phone = shipment.RecipientPhone,
                    order = $"UBHAYA-{order.Id}",
                    payment_mode = paymentMode,
                    products_desc = string.Join(", ", order.Items.Select(x => x.ProductName).Distinct()),
                    cod_amount = paymentMode == "COD" ? order.TotalAmount : 0,
                    total_amount = order.TotalAmount,
                    quantity = Math.Max(1, order.Items.Sum(x => x.Quantity)).ToString(CultureInfo.InvariantCulture),
                    weight = shipment.WeightGrams,
                    shipment_width = 20,
                    shipment_height = 5,
                    shipment_length = 25,
                    seller_name = configuration["Delhivery:SellerName"] ?? "UBHAYA FASHIONS",
                    seller_add = configuration["Delhivery:SellerAddress"] ?? "",
                    seller_pin = configuration["Delhivery:SellerPin"] ?? "",
                    seller_phone = configuration["Delhivery:SellerPhone"] ?? "",
                    shipping_mode = "Surface",
                    waybill = ""
                }
            },
            pickup_location = new { name = pickup }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("format", "json"),
            new KeyValuePair<string, string>("data", json)
        });

        // Always log non-sensitive diagnostics. Full payload logging is opt-in because
        // the payload contains customer and seller personal information.
        var httpClient = CreateClient();
        var requestUri = new Uri(httpClient.BaseAddress!, "api/cmu/create.json");
        var productsDescription = string.Join(", ", order.Items.Select(x => x.ProductName).Distinct());
        var forbiddenCharacters = new[] { '%', '#', ';', '\\\\', '&' };
        var addressSpecialCharacters = forbiddenCharacters
            .Where(character => shipment.Address.Contains(character))
            .Select(character => character == '\\\\' ? "backslash" : character.ToString())
            .ToArray();

        logger.LogInformation(
            "Delhivery Support Request Summary. OrderId={OrderId}, OrderReference={OrderReference}, Method=POST, RequestUri={RequestUri}, ContentType=application/x-www-form-urlencoded, FormFields=format,data, FormatValue=json, ClientName={ClientName}, PickupLocationName={PickupLocationName}, DestinationNamePresent={DestinationNamePresent}, DestinationPhonePresent={DestinationPhonePresent}, DestinationPin={DestinationPin}, City={City}, State={State}, Country={Country}, PaymentMode={PaymentMode}, PaymentStatus={PaymentStatus}, TotalAmount={TotalAmount}, CodAmount={CodAmount}, ProductsDescription={ProductsDescription}, Quantity={Quantity}, WeightGrams={WeightGrams}, DimensionsCm=25x20x5, ShippingMode=Surface, SellerName={SellerName}, SellerPin={SellerPin}, SellerAddressPresent={SellerAddressPresent}, SellerPhonePresent={SellerPhonePresent}, WaybillFieldIsBlank={WaybillFieldIsBlank}, AddressContainsDelhiveryFaqSpecialCharacters={AddressContainsDelhiveryFaqSpecialCharacters}, AddressSpecialCharacters={AddressSpecialCharacters}, JsonPayloadLength={JsonPayloadLength}",
            order.Id,
            $"UBHAYA-{order.Id}",
            requestUri,
            clientName,
            pickup,
            !string.IsNullOrWhiteSpace(shipment.RecipientName),
            !string.IsNullOrWhiteSpace(shipment.RecipientPhone),
            shipment.PostalCode,
            shipment.City,
            shipment.State,
            shipment.Country,
            paymentMode,
            order.PaymentStatus,
            order.TotalAmount,
            paymentMode == "COD" ? order.TotalAmount : 0,
            productsDescription,
            Math.Max(1, order.Items.Sum(x => x.Quantity)),
            shipment.WeightGrams,
            configuration["Delhivery:SellerName"] ?? "UBHAYA FASHIONS",
            configuration["Delhivery:SellerPin"] ?? "",
            !string.IsNullOrWhiteSpace(configuration["Delhivery:SellerAddress"]),
            !string.IsNullOrWhiteSpace(configuration["Delhivery:SellerPhone"]),
            true,
            addressSpecialCharacters.Length > 0,
            string.Join(",", addressSpecialCharacters),
            json.Length);

        if (configuration.GetValue<bool>("Delhivery:EnableSupportPayloadLogging"))
        {
            logger.LogWarning(
                "Delhivery FULL SUPPORT PAYLOAD ENABLED. This log contains customer and seller personal information. OrderId={OrderId}, FormFormat=json, FormData={FormData}",
                order.Id,
                json);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/cmu/create.json")
        {
            Content = content
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "NotProvided";
        var responseRequestId = response.Headers.TryGetValues("X-Request-Id", out var requestIds)
            ? string.Join(",", requestIds)
            : response.Headers.TryGetValues("X-Trace-Id", out var traceIds)
                ? string.Join(",", traceIds)
                : "NotProvided";

        // Log the complete provider response. It is essential for diagnosing HTTP
        // errors and HTTP 200 responses that contain package-level failures.
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Delhivery Create Shipment HTTP failure. OrderId={OrderId}, HttpStatus={HttpStatus}, ReasonPhrase={ReasonPhrase}, ContentType={ContentType}, ProviderRequestId={ProviderRequestId}, ResponseBody={ResponseBody}",
                order.Id,
                (int)response.StatusCode,
                response.ReasonPhrase,
                responseContentType,
                responseRequestId,
                body);
        }
        else
        {
            logger.LogInformation(
                "Delhivery Create Shipment provider response. OrderId={OrderId}, HttpStatus={HttpStatus}, ContentType={ContentType}, ProviderRequestId={ProviderRequestId}, ResponseBody={ResponseBody}",
                order.Id,
                (int)response.StatusCode,
                responseContentType,
                responseRequestId,
                body);
        }

        EnsureSuccess(response, body, "Shipment creation");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("success", out var success) &&
            success.ValueKind == JsonValueKind.False)
            throw new InvalidOperationException(ExtractShipmentError(root));

        var packageStatus = FindString(root, "status");
        if (string.Equals(packageStatus, "Fail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(ExtractShipmentError(root));

        var waybill = FindString(root, "waybill");
        if (string.IsNullOrWhiteSpace(waybill))
            throw new InvalidOperationException(ExtractShipmentError(root));

        return (waybill, FindString(root, "label_url"));
    }

    public async Task<string?> RefreshTrackingAsync(string waybill, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(waybill)) return null;
        using var response = await CreateClient().GetAsync(
            $"api/v1/packages/json/?waybill={Uri.EscapeDataString(waybill)}",
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body, "Shipment tracking");
        using var document = JsonDocument.Parse(body);
        return FindStatus(document.RootElement);
    }

    private static void ValidatePincode(string? pin, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 6 || !pin.All(char.IsDigit))
            throw new ArgumentException("A valid six-digit Indian PIN code is required.", parameterName);
    }

    private static bool IsYes(string? value) =>
        string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body, string operation)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Delhivery {operation} failed ({(int)response.StatusCode}): {body}");
    }

    private static string ExtractShipmentError(JsonElement root)
    {
        var messages = new List<string>();

        // Delhivery may return an error/message at the root, or remarks per package.
        foreach (var field in new[] { "error", "message", "rmk" })
        {
            var value = FindString(root, field);
            if (!string.IsNullOrWhiteSpace(value))
                messages.Add(value);
        }

        if (root.TryGetProperty("packages", out var packages) &&
            packages.ValueKind == JsonValueKind.Array)
        {
            foreach (var package in packages.EnumerateArray())
            {
                foreach (var field in new[] { "remarks", "error", "message" })
                {
                    if (!package.TryGetProperty(field, out var value))
                        continue;

                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String &&
                                !string.IsNullOrWhiteSpace(item.GetString()))
                                messages.Add(item.GetString()!);
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.String &&
                             !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        messages.Add(value.GetString()!);
                    }
                }
            }
        }

        return messages.Count > 0
            ? string.Join(" | ", messages.Distinct(StringComparer.OrdinalIgnoreCase))
            : root.GetRawText();
    }

    private static string SafeBody(byte[] bytes)
    {
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        return text.Length > 1200 ? text[..1200] : text;
    }

    private static string? FindStatus(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("Status", out var status))
            {
                if (status.ValueKind == JsonValueKind.Object &&
                    status.TryGetProperty("Status", out var nested) &&
                    nested.ValueKind == JsonValueKind.String) return nested.GetString();
                if (status.ValueKind == JsonValueKind.String) return status.GetString();
            }
            foreach (var property in element.EnumerateObject())
            {
                var result = FindStatus(property.Value);
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var result = FindStatus(item);
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
        }
        return null;
    }

    private static string? FindString(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
                    if (property.Value.ValueKind == JsonValueKind.Number) return property.Value.ToString();
                }
                var nested = FindString(property.Value, name);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, name);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        return null;
    }
}
