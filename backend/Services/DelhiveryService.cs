using System.Net.Http.Headers;
using System.Text.Json;
using backend.Models;

namespace backend.Services;

public interface IDelhiveryService
{
    Task<(string Waybill, string? LabelUrl)> CreateShipmentAsync(Order order, DelhiveryShipment shipment, CancellationToken cancellationToken);
    Task<string?> RefreshTrackingAsync(string waybill, CancellationToken cancellationToken);
}

public sealed class DelhiveryService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IDelhiveryService
{
    private HttpClient CreateClient()
    {
        var token = configuration["Delhivery:ApiToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Delhivery:ApiToken is not configured.");
        var client = httpClientFactory.CreateClient("Delhivery");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        return client;
    }

    public async Task<(string Waybill, string? LabelUrl)> CreateShipmentAsync(
        Order order, DelhiveryShipment shipment, CancellationToken cancellationToken)
    {
        var pickup = configuration["Delhivery:PickupLocationName"];
        if (string.IsNullOrWhiteSpace(pickup))
            throw new InvalidOperationException("Delhivery:PickupLocationName is not configured.");
        if (shipment.PostalCode.Length != 6 || !shipment.PostalCode.All(char.IsDigit))
            throw new InvalidOperationException("A valid six-digit recipient postal code is required.");
        if (shipment.RecipientPhone.Count(char.IsDigit) < 10)
            throw new InvalidOperationException("A valid recipient phone number is required.");

        var paymentMode = string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase) ? "COD" : "Pre-paid";
        if (paymentMode == "Pre-paid" && !string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prepaid shipment creation is allowed only after payment is verified.");

        var payload = new
        {
            shipments = new[]
            {
                new
                {
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
                    quantity = Math.Max(1, order.Items.Sum(x => x.Quantity)).ToString(),
                    weight = shipment.WeightGrams,
                    shipment_width = 20,
                    shipment_height = 5,
                    shipment_length = 25,
                    seller_name = configuration["Delhivery:SellerName"] ?? "UBHAYA FASHIONS",
                    seller_add = configuration["Delhivery:SellerAddress"] ?? "",
                    seller_pin = configuration["Delhivery:SellerPin"] ?? "",
                    seller_phone = configuration["Delhivery:SellerPhone"] ?? "",
                    shipping_mode = "Surface"
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
        using var response = await CreateClient().PostAsync("api/cmu/create.json", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Delhivery shipment creation failed ({(int)response.StatusCode}).");

        using var document = JsonDocument.Parse(body);
        var waybill = FindString(document.RootElement, "waybill");
        if (string.IsNullOrWhiteSpace(waybill))
            throw new InvalidOperationException(FindString(document.RootElement, "message") ??
                FindString(document.RootElement, "remarks") ?? "Delhivery did not return a waybill.");

        return (waybill, FindString(document.RootElement, "label_url"));
    }

    public async Task<string?> RefreshTrackingAsync(string waybill, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(waybill)) return null;
        using var response = await CreateClient().GetAsync(
            $"api/v1/packages/json/?waybill={Uri.EscapeDataString(waybill)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Delhivery tracking failed ({(int)response.StatusCode}).");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return FindStatus(document.RootElement);
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