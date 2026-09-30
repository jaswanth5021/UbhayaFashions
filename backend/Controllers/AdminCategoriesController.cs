using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController(ApplicationDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CategoryRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) return BadRequest("Category name is required.");
        if (name.Length > 250) return BadRequest("Category name must be 250 characters or fewer.");
        if (await db.Categories.AnyAsync(category => category.Name.ToLower() == name.ToLower()))
            return Conflict("A category with this name already exists.");

        var homeError = await ValidateHomePageSelection(request.ShowOnHomePage);
        if (homeError is not null) return BadRequest(homeError);

        var category = new Category { Name = name, ShowOnHomePage = request.ShowOnHomePage, ImageUrl = request.ImageUrl?.Trim() ?? string.Empty };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) return BadRequest("Category name is required.");
        if (name.Length > 250) return BadRequest("Category name must be 250 characters or fewer.");

        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        if (await db.Categories.AnyAsync(other => other.Id != id && other.Name.ToLower() == name.ToLower()))
            return Conflict("A category with this name already exists.");

        var homeError = await ValidateHomePageSelection(request.ShowOnHomePage, id);
        if (homeError is not null) return BadRequest(homeError);

        var previousImageUrl = category.ImageUrl;
        category.Name = name;
        category.ShowOnHomePage = request.ShowOnHomePage;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl)) category.ImageUrl = request.ImageUrl.Trim();
        await db.SaveChangesAsync();
        if (!string.Equals(previousImageUrl, category.ImageUrl, StringComparison.Ordinal)) DeleteStoredImage(previousImageUrl);
        return Ok(category);
    }

    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(10000000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        if (file is null || file.Length == 0 || file.Length > 10000000) return BadRequest("Choose an image smaller than 10 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension)) return BadRequest("Only JPG, JPEG, PNG and WEBP images are supported.");

        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "uploads", "categories", id.ToString());
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName))) await file.CopyToAsync(stream);

        var previousImageUrl = category.ImageUrl;
        category.ImageUrl = $"uploads/categories/{id}/{fileName}";
        await db.SaveChangesAsync();
        DeleteStoredImage(previousImageUrl);
        return Ok(new { imageUrl = category.ImageUrl });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        if (await db.Products.AnyAsync(product => product.CategoryId == id))
            return Conflict("This category is assigned to products. Reassign those products before deleting it.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        DeleteStoredImage(category.ImageUrl);
        return NoContent();
    }

    private async Task<string?> ValidateHomePageSelection(bool showOnHomePage, int? currentId = null)
    {
        if (!showOnHomePage) return null;
        var selectedCount = await db.Categories.CountAsync(category => category.ShowOnHomePage && (!currentId.HasValue || category.Id != currentId.Value));
        return selectedCount >= 4 ? "You can display up to four categories on the home page." : null;
    }

    private void DeleteStoredImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || Uri.TryCreate(imageUrl, UriKind.Absolute, out _)) return;
        var relativePath = imageUrl.Replace('\\', '/').TrimStart('/');
        if (!relativePath.StartsWith("uploads/categories/", StringComparison.OrdinalIgnoreCase)) return;
        var path = Path.GetFullPath(Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), relativePath));
        var root = Path.GetFullPath(Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "categories")) + Path.DirectorySeparatorChar;
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
