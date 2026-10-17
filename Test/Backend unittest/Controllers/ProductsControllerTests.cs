using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class ProductsControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"products-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAll_WithNoProducts_ReturnsEmptyCollection()
    {
        await using var db = CreateContext();

        var result = Assert.IsType<OkObjectResult>(
            await new ProductsController(db).GetAll());
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<Product>>(result.Value));
    }

    [Fact]
    public async Task GetColors_ReturnsDistinctCanonicalColors()
    {
        await using var db = CreateContext();
        db.Products.AddRange(
            new Product { Name = "A", Colors = "Pink,Blue" },
            new Product { Name = "B", Colors = "pink,Gold" });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await new ProductsController(db).GetColors());
        var colors = Assert.IsAssignableFrom<IEnumerable<string>>(result.Value);

        Assert.Equal(new[] { "Black", "White", "Red", "Pink", "Blue", "Gold" }
            .Where(expected => colors.Contains(expected)),
            colors);
        Assert.Equal(3, colors.Count());
        Assert.Contains("Pink", colors);
        Assert.Contains("Blue", colors);
        Assert.Contains("Gold", colors);
    }

    [Fact]
    public async Task GetPriceBounds_UsesDefaultCeilingWhenNoVariantsExist()
    {
        await using var db = CreateContext();

        var result = Assert.IsType<OkObjectResult>(
            await new ProductsController(db).GetPriceBounds());
        var value = result.Value!;

        Assert.Equal(0, (int)value.GetType().GetProperty("min")!.GetValue(value)!);
        Assert.Equal(10000, (int)value.GetType().GetProperty("max")!.GetValue(value)!);
    }

    [Fact]
    public async Task Get_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateContext();

        Assert.IsType<NotFoundResult>(await new ProductsController(db).Get(123));
    }

    [Fact]
    public async Task GetRelated_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateContext();

        Assert.IsType<NotFoundResult>(await new ProductsController(db).GetRelated(123));
    }

    [Fact]
    public async Task GetNewarrivals_WhenNoProductsExist_ReturnsEmptyCollection()
    {
        await using var db = CreateContext();

        var result = Assert.IsType<OkObjectResult>(
            await new ProductsController(db).GetNewarrivals());

        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<Product>>(result.Value));
    }

    [Fact]
    public async Task Create_ReturnsBadRequestWhenCategoryDoesNotExist()
    {
        await using var db = CreateContext();
        var product = new Product { Name = "Summer Dress", CategoryId = 999 };

        var result = Assert.IsType<BadRequestObjectResult>(
            await new ProductsController(db).Create(product));

        Assert.Equal("Choose a valid category.", result.Value);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task Create_WithValidCategoryReturnsCreatedProduct()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Dresses" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var product = new Product
        {
            Name = "Summer Dress",
            CategoryId = category.Id,
            Colors = "Pink",
            ImageUrl = "/uploads/summer.jpg"
        };

        var result = Assert.IsType<CreatedAtActionResult>(
            await new ProductsController(db).Create(product));

        Assert.Equal(nameof(ProductsController.Get), result.ActionName);
        Assert.Equal(category.Id, Assert.IsType<Product>(result.Value).CategoryId);
        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Update_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateContext();

        Assert.IsType<NotFoundResult>(await new ProductsController(db).Update(
            123, new Product { Name = "Updated" }));
    }

    [Fact]
    public async Task Update_ReturnsBadRequestWhenCategoryIsInvalid()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Dresses" };
        var product = new Product { Name = "Old name", CategoryNavigation = category };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = Assert.IsType<BadRequestObjectResult>(
            await new ProductsController(db).Update(product.Id,
                new Product { Name = "New name", CategoryId = 999 }));

        Assert.Equal("Choose a valid category.", result.Value);
        Assert.Equal("Old name", (await db.Products.FindAsync(product.Id))!.Name);
    }

    [Fact]
    public async Task Update_ChangesEditableProductFields()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Dresses" };
        db.Categories.Add(category);
        var product = new Product
        {
            Name = "Old name",
            Description = "Old description",
            CategoryId = 1,
            CategoryNavigation = category,
            Colors = "Pink",
            ImageUrl = "/old.jpg"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await new ProductsController(db).Update(product.Id, new Product
            {
                Name = "New name",
                Description = "New description",
                CategoryId = category.Id,
                Colors = "Blue",
                ImageUrl = "/new.jpg"
            }));

        var updated = Assert.IsType<Product>(result.Value);
        Assert.Equal("New name", updated.Name);
        Assert.Equal("New description", updated.Description);
        Assert.Equal("Blue", updated.Colors);
        Assert.Equal("/new.jpg", updated.ImageUrl);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateContext();

        Assert.IsType<NotFoundResult>(await new ProductsController(db).Delete(123));
    }

    [Fact]
    public async Task Delete_RemovesExistingProductAndReturnsNoContent()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Dresses" };
        db.Categories.Add(category);
        var product = new Product { Name = "Summer Dress", CategoryNavigation = category };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await new ProductsController(db).Delete(product.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await db.Products.ToListAsync());
    }
}
