using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/storefront-banner")]
[Authorize(Roles = "Admin")]
public sealed class StorefrontBannerController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxImageSize = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    // Keep banner uploads outside the publish directory. The configured UploadsPath
    // should be shared with the frontend; the Windows fallback preserves existing
    // EC2 files under C:\\UbhayaFashions\\Uploads.
    private string UploadDirectory
    {
        get
        {
            var configured = configuration["UploadsPath"];
            var path = string.IsNullOrWhiteSpace(configured)
                ? (OperatingSystem.IsWindows()
                    ? @"C:\UbhayaFashions\Uploads"
                    : Path.Combine(environment.ContentRootPath, "Uploads"))
                : configured;
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }

    private string MetadataPath => Path.Combine(UploadDirectory, "storefront-banners.json");

    public sealed class BannerEntry
    {
        public string FileName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
        public string ImageUrl => "/uploads/" + FileName;
    }

    public sealed class BannerStatusRequest
    {
        public bool IsActive { get; set; }
    }

    private List<BannerEntry> ReadBanners()
    {
        if (System.IO.File.Exists(MetadataPath))
        {
            try
            {
                return JsonSerializer.Deserialize<List<BannerEntry>>(System.IO.File.ReadAllText(MetadataPath), JsonOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        // Migrate the previous single-banner upload so it remains visible after upgrading.
        var legacy = Directory.EnumerateFiles(UploadDirectory, "dress-banner.*")
            .Where(path => AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new BannerEntry
            {
                FileName = Path.GetFileName(path),
                IsActive = true,
                UploadedAtUtc = System.IO.File.GetLastWriteTimeUtc(path)
            }).ToList();

        if (legacy.Count > 0)
            WriteBanners(legacy);

        return legacy;
    }

    private void WriteBanners(List<BannerEntry> banners)
        => System.IO.File.WriteAllText(MetadataPath, JsonSerializer.Serialize(banners, JsonOptions));

    [HttpGet]
    public IActionResult List()
        => Ok(ReadBanners().OrderByDescending(banner => banner.UploadedAtUtc));

    [HttpPost]
    [RequestSizeLimit(60 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile>? banners, CancellationToken cancellationToken)
    {
        if (banners is null || banners.Count == 0)
            return BadRequest("Choose one or more banner images to upload.");

        var existing = ReadBanners();
        foreach (var banner in banners)
        {
            if (banner is null || banner.Length == 0)
                continue;
            if (banner.Length > MaxImageSize)
                return BadRequest($"Each banner image must be 10 MB or smaller: {Path.GetFileName(banner.FileName)}");

            var extension = Path.GetExtension(banner.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Upload JPG, PNG, or WEBP images only.");
            if (!banner.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Each selected file must be an image.");

            var fileName = $"banner-{Guid.NewGuid():N}{extension}";
            var destination = Path.Combine(UploadDirectory, fileName);
            await using var stream = System.IO.File.Create(destination);
            await banner.CopyToAsync(stream, cancellationToken);
            existing.Add(new BannerEntry { FileName = fileName, IsActive = true, UploadedAtUtc = DateTime.UtcNow });
        }

        if (existing.Count == 0)
            return BadRequest("No valid banner images were uploaded.");

        WriteBanners(existing);
        return Ok(new { message = "Banner images uploaded.", banners = existing.OrderByDescending(b => b.UploadedAtUtc) });
    }

    [HttpPut("{fileName}/status")]
    public IActionResult UpdateStatus(string fileName, [FromBody] BannerStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return BadRequest("Invalid banner filename.");

        var banners = ReadBanners();
        var banner = banners.FirstOrDefault(item => string.Equals(item.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (banner is null)
            return NotFound("Banner image not found.");

        banner.IsActive = request.IsActive;
        WriteBanners(banners);
        return Ok(new { success = true, banner });
    }

    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return BadRequest("Invalid banner filename.");

        var banners = ReadBanners();
        var banner = banners.FirstOrDefault(item =>
            string.Equals(item.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (banner is null)
            return NotFound("Banner image not found.");

        var imagePath = Path.Combine(UploadDirectory, banner.FileName);
        banners.Remove(banner);
        WriteBanners(banners);

        if (System.IO.File.Exists(imagePath))
            System.IO.File.Delete(imagePath);

        return Ok(new { success = true, message = "Banner image deleted." });
    }

    [AllowAnonymous]
    [HttpGet("/api/storefront/banner")]
    public IActionResult Current()
    {
        var active = ReadBanners()
            .Where(banner => banner.IsActive && System.IO.File.Exists(Path.Combine(UploadDirectory, banner.FileName)))
            .OrderBy(banner => banner.UploadedAtUtc)
            .Select(banner => banner.ImageUrl)
            .ToList();

        return Ok(new { imageUrl = active.FirstOrDefault(), imageUrls = active });
    }
}
