using System.Globalization;
using System.Text;
using System.Xml;
using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

[ApiController]
[Route("feeds/google-products.xml")]
public sealed class GoogleProductFeedController(
    ApiService api,
    IConfiguration configuration,
    ILogger<GoogleProductFeedController> logger) : ControllerBase
{
    private const string GoogleNamespace = "http://base.google.com/ns/1.0";

    [HttpGet]
    [Produces("application/xml")]
    public async Task<IActionResult> Index()
    {
        var configuredSiteUrl = configuration["PublicSiteUrl"];
        var siteRoot = (string.IsNullOrWhiteSpace(configuredSiteUrl)
                ? "https://ubhayafashions.in"
                : configuredSiteUrl)
            .Trim().TrimEnd('/') + "/";

        if (!Uri.TryCreate(siteRoot, UriKind.Absolute, out var siteUri)
            || siteUri.Scheme != Uri.UriSchemeHttps)
        {
            logger.LogError("PublicSiteUrl must be a valid HTTPS URL to generate the Google product feed.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        List<ProductViewModel> products;
        try
        {
            products = await api.GetProductsAsync();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            logger.LogError(exception, "Could not load products while generating the Google Merchant Center feed.");
            Response.Headers["Retry-After"] = "300";
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "rss", null);
            await writer.WriteAttributeStringAsync(null, "version", null, "2.0");
            await writer.WriteAttributeStringAsync("xmlns", "g", null, GoogleNamespace);
            await writer.WriteStartElementAsync(null, "channel", null);
            await writer.WriteElementStringAsync(null, "title", null, "Ubhaya Fashions");
            await writer.WriteElementStringAsync(null, "link", null, siteUri.AbsoluteUri);
            await writer.WriteElementStringAsync(null, "description", null, "Women's dresses and ethnic wear from Ubhaya Fashions.");

            foreach (var product in products
                .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Name)))
            {
                var imagePath = !string.IsNullOrWhiteSpace(product.ImageUrl)
                    ? product.ImageUrl
                    : product.Images.OrderBy(image => image.SortOrder)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

                var resolvedImage = ImageUrlHelper.Resolve(imagePath, configuration["ApiBaseUrl"]);
                var imageUrl = MakeAbsoluteUrl(siteUri, resolvedImage);
                if (imageUrl is null)
                    continue;

                var productUrl = new Uri(siteUri, $"/Products/Details/{product.Id}").AbsoluteUri;
                var variants = product.Variants
                    .Where(variant => variant.Id > 0
                        && !string.IsNullOrWhiteSpace(variant.Size)
                        && variant.Price > 0)
                    .ToList();

                foreach (var variant in variants)
                {
                    var regularPrice = variant.Discount > 0 && variant.Discount < 100
                        ? Math.Round(variant.Price / (1m - variant.Discount / 100m), 2)
                        : variant.Price;
                    var salePrice = variant.Discount > 0 && variant.Discount < 100
                        ? variant.Price
                        : (decimal?)null;

                    await writer.WriteStartElementAsync(null, "item", null);
                    await WriteGoogleField(writer, "id", $"{product.Id}-{variant.Id}");
                    await WriteGoogleField(writer, "item_group_id", product.Id.ToString(CultureInfo.InvariantCulture));
                    await WriteGoogleField(writer, "title", $"{product.Name} - {variant.Size}");
                    await WriteGoogleField(writer, "description", string.IsNullOrWhiteSpace(product.Description)
                        ? $"{product.Name} from Ubhaya Fashions."
                        : product.Description);
                    await WriteGoogleField(writer, "link", productUrl);
                    await WriteGoogleField(writer, "image_link", imageUrl);
                    await WriteGoogleField(writer, "availability", variant.Stock > 0 ? "in_stock" : "out_of_stock");
                    await WriteGoogleField(writer, "condition", "new");
                    await WriteGoogleField(writer, "price", FormatPrice(regularPrice));
                    if (salePrice.HasValue)
                        await WriteGoogleField(writer, "sale_price", FormatPrice(salePrice.Value));
                    await WriteGoogleField(writer, "brand", "Ubhaya Fashions");
                    await WriteGoogleField(writer, "google_product_category", "Apparel & Accessories > Clothing");
                    await WriteGoogleField(writer, "product_type", product.Category);
                    await WriteGoogleField(writer, "size", variant.Size);
                    var color = string.IsNullOrWhiteSpace(variant.Color) ? product.Colors : variant.Color;
                    if (!string.IsNullOrWhiteSpace(color))
                        await WriteGoogleField(writer, "color", color);
                    await WriteGoogleField(writer, "identifier_exists", "no");
                    await writer.WriteEndElementAsync();
                }
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
        }

        return File(stream.ToArray(), "application/xml; charset=utf-8");
    }

    private static async Task WriteGoogleField(XmlWriter writer, string name, string value)
        => await writer.WriteElementStringAsync("g", name, GoogleNamespace, value);

    private static string FormatPrice(decimal price)
        => $"{price.ToString("0.00", CultureInfo.InvariantCulture)} INR";

    private static string? MakeAbsoluteUrl(Uri siteRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!Uri.TryCreate(siteRoot, path, out var absolute)
            || absolute.Scheme != Uri.UriSchemeHttps)
            return null;

        return absolute.AbsoluteUri;
    }
}
