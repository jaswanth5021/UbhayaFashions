using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/storefront-banner")]
[Authorize(Roles = "Admin")]
public sealed class StorefrontBannerController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    private string UploadDirectory
    {
        get
        {
            var configured = configuration["UploadsPath"];
            var path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "Uploads")
                : configured;
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? banner, CancellationToken cancellationToken)
    {
        if (banner is null || banner.Length == 0)
            return BadRequest("Choose a banner image to upload.");
        if (banner.Length > 10 * 1024 * 1024)
            return BadRequest("The banner image must be 10 MB or smaller.");

        var extension = Path.GetExtension(banner.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest("Upload a JPG, PNG, or WEBP image.");

        if (!banner.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("The selected file must be an image.");

        var directory = UploadDirectory;
        foreach (var existing in Directory.EnumerateFiles(directory, "dress-banner.*"))
            System.IO.File.Delete(existing);

        var fileName = "dress-banner" + extension;
        var destination = Path.Combine(directory, fileName);
        await using (var stream = System.IO.File.Create(destination))
            await banner.CopyToAsync(stream, cancellationToken);

        return Ok(new { imageUrl = "/uploads/" + fileName, message = "Homepage banner updated." });
    }

    [AllowAnonymous]
    [HttpGet("/api/storefront/banner")]
    public IActionResult Current()
    {
        var directory = UploadDirectory;
        var image = Directory.EnumerateFiles(directory, "dress-banner.*")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(path => new[] { ".jpg", ".jpeg", ".png", ".webp" }
                .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));

        return Ok(new { imageUrl = image is null ? null : "/uploads/" + Path.GetFileName(image) });
    }
}
