using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace UbhayaFashions.Automation.Tests;

public sealed class WebsiteSmokeTests : PageTest
{
    [Fact]
    public async Task HomePage_LoadsSuccessfully()
    {
        var response = await Page.GotoAsync(GetWebsiteUrl());

        Assert.NotNull(response);
        Assert.True(
            (int)response.Status < 400,
            $"Homepage returned HTTP {(int)response.Status}.");

        var title = await Page.TitleAsync();
        Assert.False(string.IsNullOrWhiteSpace(title), "Homepage should have a non-empty title.");
    }

    [Fact]
    public async Task ProductsPage_LoadsSuccessfully()
    {
        var productsUrl = new Uri(new Uri(GetWebsiteUrl()), "Products");
        var response = await Page.GotoAsync(productsUrl.ToString());

        Assert.NotNull(response);
        Assert.True(
            (int)response.Status < 400,
            $"Products page returned HTTP {(int)response.Status}.");
    }

    private static string GetWebsiteUrl()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("UBHAYA_UI_BASE_URL");

        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            throw new InvalidOperationException(
                "Set UBHAYA_UI_BASE_URL to the MVC website base URL, for example https://shop.example.com/.");
        }

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "UBHAYA_UI_BASE_URL must be an absolute HTTP or HTTPS URL.");
        }

        return uri.ToString().TrimEnd('/') + "/";
    }
}
