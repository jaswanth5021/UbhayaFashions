using System.Net;
using System.Text.Json;
using Xunit;

namespace UbhayaFashions.Automation.Tests;

public sealed class ApiSmokeTests
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip
    };

    [Fact]
    public async Task GetProducts_ReturnsSuccessAndJsonArray()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetProductColors_ReturnsSuccessAndJsonArray()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("api/products/colors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetPriceBounds_ReturnsNumericMinimumAndMaximum()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("api/products/price-bounds");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("min", out var minimum));
        Assert.True(root.TryGetProperty("max", out var maximum));
        Assert.True(minimum.TryGetDecimal(out var minValue));
        Assert.True(maximum.TryGetDecimal(out var maxValue));
        Assert.True(minValue >= 0);
        Assert.True(maxValue >= minValue);
    }

    [Fact]
    public async Task GetUnknownProduct_ReturnsNotFound()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("api/products/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRelatedProductsForUnknownProduct_ReturnsNotFound()
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync("api/products/2147483647/related");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("api/products?search=UBHAYA")]
    [InlineData("api/products?sort=price-low")]
    [InlineData("api/products?availability=in-stock")]
    [InlineData("api/products?availability=out-of-stock")]
    public async Task ProductListQueries_ReturnJsonArrays(string relativeUrl)
    {
        using var client = CreateApiClient();
        using var response = await client.GetAsync(relativeUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json, JsonOptions);
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
