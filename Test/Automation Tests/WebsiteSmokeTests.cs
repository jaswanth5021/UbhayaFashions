using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace UbhayaFashions.Automation.Tests;

public sealed class WebsiteSmokeTests : PageTest
{
    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/Products", "Products")]
    [InlineData("/Products/Categories", "Categories")]
    [InlineData("/Account/Login", "Login")]
    [InlineData("/Cart", "Cart")]
    public async Task PublicPage_LoadsWithoutServerError(string path, string pageName)
    {
        var targetUrl = new Uri(new Uri(GetWebsiteUrl()), path.TrimStart('/'));
        var response = await Page.GotoAsync(targetUrl.ToString());

        Assert.NotNull(response);
        Assert.True(
            (int)response.Status < 400,
            $"{pageName} page returned HTTP {(int)response.Status}.");

        // Check the page has rendered meaningful document content, not a blank response.
        await Page.Locator("body").WaitForAsync();
        var bodyText = (await Page.Locator("body").InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(bodyText), $"{pageName} page body should not be empty.");
    }

    [Fact]
    public async Task HomePage_HasNonEmptyTitle()
    {
        await Page.GotoAsync(GetWebsiteUrl());

        var title = await Page.TitleAsync();

        Assert.False(string.IsNullOrWhiteSpace(title), "Homepage should have a non-empty title.");
    }

    [Fact]
    public async Task ProductsPage_HasDocumentBody()
    {
        var productsUrl = new Uri(new Uri(GetWebsiteUrl()), "Products");
        await Page.GotoAsync(productsUrl.ToString());

        Assert.True(await Page.Locator("body").CountAsync() == 1);
    }

    [Fact]
    public async Task HomePage_CanRenderAtMobileViewport()
    {
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 812 });
        var response = await Page.GotoAsync(GetWebsiteUrl());

        Assert.NotNull(response);
        Assert.True((int)response.Status < 400, "Homepage should load at a mobile viewport.");

        var documentWidth = await Page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>(
            "() => document.documentElement.clientWidth");

        Assert.True(
            documentWidth <= viewportWidth + 1,
            $"Homepage has horizontal overflow: document width {documentWidth}px, viewport width {viewportWidth}px.");
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
