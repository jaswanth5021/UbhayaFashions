using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
        => Ok(await db.Products.AsNoTracking().Include(x => x.CategoryNavigation).Include(x => x.Images).Include(x => x.Videos).Include(x => x.Variants).OrderByDescending(x => x.Id).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await db.Products.AsNoTracking().Include(x => x.CategoryNavigation).Include(x => x.Images).Include(x => x.Videos).Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminProductRequest request)
    {
        var validation = ValidateVariants(request.Variants);
        if (validation is not null) return BadRequest(validation);
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Product name is required.");
        if (!IsValidImageUrl(request.ImageUrl)) return BadRequest("Image URL must be an absolute HTTP or HTTPS URL.");
        var category = await db.Categories.FindAsync(request.CategoryId);
        if (category is null) return BadRequest("Choose a valid category.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CategoryId = category.Id,
            CategoryNavigation = category,
            Colors = request.Colors?.Trim() ?? string.Empty,
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            IsBestSeller = request.IsBestSeller,
            Variants = request.Variants.Select(ToVariant).ToList()
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        foreach (var variant in product.Variants.Where(variant => variant.Stock > 0))
            AddInventory(product.Id, variant.Size, "Stock In", variant.Stock, variant.Stock, "Initial stock");
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdminProductRequest request)
    {
        var validation = ValidateVariants(request.Variants);
        if (validation is not null) return BadRequest(validation);
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Product name is required.");
        if (request.CategoryId <= 0) return BadRequest("Choose a category before saving the product.");
        if (!IsValidImageUrl(request.ImageUrl, id)) return BadRequest("Enter an absolute HTTP or HTTPS image URL, or keep the existing image.");
        var category = await db.Categories.FindAsync(request.CategoryId);
        if (category is null) return BadRequest("Choose a valid category.");

        var product = await db.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == id);
        if (product is null) return NotFound();

        var previousStock = product.Variants.ToDictionary(x => x.Size, x => x.Stock, StringComparer.OrdinalIgnoreCase);
        var previousImageUrl = product.ImageUrl;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.CategoryId = category.Id;
        product.CategoryNavigation = category;
        product.Colors = request.Colors?.Trim() ?? string.Empty;
        product.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        product.IsBestSeller = request.IsBestSeller;
        db.ProductVariants.RemoveRange(product.Variants);
        var updatedVariants = request.Variants.Select(ToVariant).ToList();
        db.ProductVariants.AddRange(updatedVariants);
        product.Variants = updatedVariants;
        await db.SaveChangesAsync();
        if (!string.Equals(previousImageUrl, product.ImageUrl, StringComparison.Ordinal))
            DeleteStoredImage(id, previousImageUrl);

        foreach (var variant in product.Variants)
        {
            previousStock.TryGetValue(variant.Size, out var oldStock);
            if (oldStock != variant.Stock)
                AddInventory(product.Id, variant.Size, "Adjustment", variant.Stock - oldStock, variant.Stock, "Variant stock edited by admin");
        }
        await db.SaveChangesAsync();
        return Ok(await db.Products.AsNoTracking().Include(x => x.CategoryNavigation).Include(x => x.Images).Include(x => x.Videos).Include(x => x.Variants).FirstAsync(x => x.Id == id));
    }

    [HttpPost("{id:int}/bestseller")]
    public async Task<IActionResult> SetBestSeller(int id, [FromBody] bool isBestSeller)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsBestSeller = isBestSeller;
        await db.SaveChangesAsync();
        return Ok(new { product.Id, product.IsBestSeller });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(10000000)]
    public async Task<IActionResult> MainImage(int id, IFormFile file)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        var imageUrl = await SaveImage(id, file);
        if (imageUrl is null) return BadRequest("Choose a JPG, JPEG, PNG or WEBP image.");
        var previousImageUrl = product.ImageUrl;
        product.ImageUrl = imageUrl;
        await db.SaveChangesAsync();
        DeleteStoredImage(id, previousImageUrl);
        return Ok(new { imageUrl });
    }

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(50000000)]
    public async Task<IActionResult> AdditionalImages(int id, List<IFormFile> files)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        if (files is null || files.Count == 0) return BadRequest("Choose at least one additional image.");
        if (files.Any(file => file.Length > 10000000)) return BadRequest("Each image must be 10 MB or smaller.");
        var nextSortOrder = await db.ProductImages.Where(image => image.ProductId == id).Select(image => (int?)image.SortOrder).MaxAsync() ?? 0;
        var addedUrls = new List<string>();
        foreach (var file in files)
        {
            var imageUrl = await SaveImage(id, file);
            if (imageUrl is null) return BadRequest("Only JPG, JPEG, PNG and WEBP images are supported.");
            db.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = imageUrl, SortOrder = ++nextSortOrder });
            addedUrls.Add(imageUrl);
        }
        await db.SaveChangesAsync();
        return Ok(addedUrls);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteAdditionalImage(int id, int imageId)
    {
        var image = await db.ProductImages.FirstOrDefaultAsync(item => item.Id == imageId && item.ProductId == id);
        if (image is null) return NotFound(new { error = $"Additional image {imageId} was not found for product {id}." });

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
        DeleteStoredImage(id, image.ImageUrl);
        return NoContent();
    }

    [HttpPost("{id:int}/videos")]
    [RequestSizeLimit(510_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 510_000_000)]
    public async Task<IActionResult> ProductVideos(int id, List<IFormFile> files)
    {
        if (!await db.Products.AnyAsync(product => product.Id == id)) return NotFound();
        if (files is null || files.Count == 0) return BadRequest("Choose at least one video.");
        if (files.Count > 5) return BadRequest("Upload no more than five videos at a time.");
        if (files.Any(file => file.Length > 100_000_000)) return BadRequest("Each video must be 100 MB or smaller.");

        var nextSortOrder = await db.ProductVideos.Where(video => video.ProductId == id).Select(video => (int?)video.SortOrder).MaxAsync() ?? 0;
        var addedUrls = new List<string>();
        foreach (var file in files)
        {
            var videoUrl = await SaveVideo(file);
            if (videoUrl is null) return BadRequest("Only MP4, WebM, and Ogg videos are supported.");
            db.ProductVideos.Add(new ProductVideo { ProductId = id, VideoUrl = videoUrl, SortOrder = ++nextSortOrder });
            addedUrls.Add(videoUrl);
        }
        await db.SaveChangesAsync();
        return Ok(addedUrls);
    }

    private async Task<string?> SaveVideo(IFormFile file)
    {
        if (file is null || file.Length == 0 || file.Length > 100_000_000) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".mp4", ".webm", ".ogv", ".ogg" }.Contains(extension)) return null;
        var folder = Path.Combine(config["UploadsPath"] ?? @"C:\UbhayaFashions\Uploads", "products", "videos");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, name));
        await file.CopyToAsync(stream);
        var baseUrl = config["PublicApiUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/uploads/products/videos/{name}";
    }
    private static string? ValidateVariants(List<AdminProductVariantRequest>? variants)
    {
        if (variants is null || variants.Count == 0) return "Add at least one size variant.";
        if (variants.Any(variant => string.IsNullOrWhiteSpace(variant.Size))) return "Every variant must have a size.";
        if (variants.GroupBy(variant => variant.Size.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)) return "Each size can only be added once.";
        if (variants.Any(variant => variant.Price < 0 || variant.Discount < 0 || variant.Discount > 100 || variant.Stock < 0)) return "Variant price, discount, or stock is invalid.";
        return null;
    }

    private static ProductVariant ToVariant(AdminProductVariantRequest variant)
        => new() { Size = variant.Size.Trim().ToUpperInvariant(), Price = variant.Price, Discount = variant.Discount, Stock = variant.Stock };

    private static bool IsValidImageUrl(string? value, int? productId = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        if (productId is null) return false;
        var path = value.Replace('\\', '/').TrimStart('/');
        var expectedPrefix = $"uploads/products/{productId.Value}/";
        if (!path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)) return false;
        var fileName = path[expectedPrefix.Length..];
        return !string.IsNullOrWhiteSpace(fileName)
            && !fileName.Contains('/')
            && fileName is not "." and not "..";
    }

    private void AddInventory(int productId, string size, string type, int quantity, int after, string note)
        => db.InventoryTransactions.Add(new InventoryTransaction { ProductId = productId, Size = size, Type = type, Quantity = quantity, StockAfter = after, Note = note });

    private async Task<string?> SaveImage(int productId, IFormFile file)
    {
        if (file is null || file.Length == 0 || file.Length > 10000000) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension)) return null;
        var folder = Path.Combine(config["UploadsPath"] ?? @"C:\UbhayaFashions\Uploads", "products", productId.ToString());
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, name));
        await file.CopyToAsync(stream);
        return $"uploads/products/{productId}/{name}";
    }

    private void DeleteStoredImage(int productId, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        string imagePath;
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            imagePath = Uri.UnescapeDataString(absoluteUri.AbsolutePath).TrimStart('/');
        }
        else
        {
            imagePath = imageUrl.TrimStart('/').Replace('\\', '/');
        }

        const string productImagesPrefix = "uploads/products/";
        if (!imagePath.StartsWith(productImagesPrefix, StringComparison.OrdinalIgnoreCase)) return;

        var relativeFile = imagePath[productImagesPrefix.Length..];
        var segments = relativeFile.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var isCurrentProductImage = segments.Length == 2
            && string.Equals(segments[0], productId.ToString(), StringComparison.Ordinal);
        var isLegacyProductImage = segments.Length == 1;
        if (!isCurrentProductImage && !isLegacyProductImage) return;
        if (segments.Any(segment => segment is "." or "..")) return;

        var productsFolder = Path.GetFullPath(Path.Combine(config["UploadsPath"] ?? @"C:\UbhayaFashions\Uploads", "products"));
        var filePath = Path.GetFullPath(Path.Combine(productsFolder, Path.Combine(segments)));
        if (!filePath.StartsWith(productsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
