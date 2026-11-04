using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

[ApiController]
[Route("sitemap.xml")]
public sealed class SitemapController(
    ApiService api,
    IConfiguration configuration,
    ILogger<SitemapController> logger) : ControllerBase
{
    [HttpGet]
    [Produces("application/xml")]
    public async Task<IActionResult> Index()
    {
        var configuredSiteUrl = configuration["PublicSiteUrl"];
        var siteRoot = (string.IsNullOrWhiteSpace(configuredSiteUrl)
                ? "https://ubhayafashions.in"
                : configuredSiteUrl)
            .Trim()
            .TrimEnd('/') + "/";

        if (!Uri.TryCreate(siteRoot, UriKind.Absolute, out var siteUri)
            || (siteUri.Scheme != Uri.UriSchemeHttps && siteUri.Scheme != Uri.UriSchemeHttp))
        {
            logger.LogError("The configured PublicSiteUrl value is invalid.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        List<ProductViewModel> products;
        List<CategoryViewModel> categories;

        try
        {
            // The storefront API is the source of truth for public catalog data.
            // It returns the complete catalog; pagination is applied by the MVC page.
            products = await api.GetProductsAsync();
            categories = await api.GetCategoriesAsync();
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or JsonException
            or TaskCanceledException)
        {
            logger.LogError(exception, "Could not load catalog data while generating the XML sitemap.");
            Response.Headers["Retry-After"] = "300";
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var entries = new List<SitemapEntry>
        {
            new("/", null),
            new("/Products", null),
            new("/Products/Categories", null),
            new("/Home/About", null),
            new("/Home/Privacy", null),
            new("/Home/Terms", null),
            new("/Home/Returns", null),
            new("/Home/Cancellation", null)
        };

        entries.AddRange(categories
            .Where(category => category.Id > 0 && !string.IsNullOrWhiteSpace(category.Name))
            .Select(category => new SitemapEntry(
                $"/Products?category={Uri.EscapeDataString(category.Name.Trim())}",
                null)));

        entries.AddRange(products
            .Where(product => product.Id > 0)
            .Select(product => new SitemapEntry(
                $"/Products/Details/{product.Id}",
                product.UpdatedDate ?? (product.CreatedDate == default ? null : product.CreatedDate))));

        var uniqueEntries = entries
            .GroupBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        await using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(
                null,
                "urlset",
                "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var entry in uniqueEntries)
            {
                var absoluteUrl = new Uri(siteUri, entry.Path).AbsoluteUri;
                await writer.WriteStartElementAsync(null, "url", null);
                await writer.WriteElementStringAsync(null, "loc", null, absoluteUrl);

                if (entry.LastModified.HasValue)
                {
                    await writer.WriteElementStringAsync(
                        null,
                        "lastmod",
                        null,
                        entry.LastModified.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                await writer.WriteEndElementAsync();
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
        }

        return File(stream.ToArray(), "application/xml; charset=utf-8");
    }

    private sealed record SitemapEntry(string Path, DateTime? LastModified);
}
