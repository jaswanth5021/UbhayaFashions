using System.Net;
using System.Text.Json;
using Xunit;

namespace UbhayaFashions.Automation.Tests;

public sealed class ApiSmokeTests
{
    [Fact]
    public async Task GetProducts_ReturnsSuccessAndJsonArray()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync("api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetProductColors_ReturnsSuccessAndJsonArray()
    {
        using var client = CreateApiClient();

        using var response = await client.GetAsync("api/products/colors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    private static HttpClient CreateApiClient()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("UBHAYA_API_BASE_URL");

        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            throw new InvalidOperationException(
                "Set UBHAYA_API_BASE_URL to the backend API root URL, for example https://api.example.com/.");
        }

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "UBHAYA_API_BASE_URL must be an absolute HTTP or HTTPS URL.");
        }

        return new HttpClient
        {
            BaseAddress = new Uri(baseUri.ToString().TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
