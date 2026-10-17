using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class CategoriesControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"categories-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task All_ReturnsCategoriesSortedByName()
    {
        await using var db = CreateContext();
        db.Categories.AddRange(
            new Category { Name = "Zebra" },
            new Category { Name = "Blouse" },
            new Category { Name = "Saree" });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await new CategoriesController(db).All());
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value);

        Assert.Equal(new[] { "Blouse", "Saree", "Zebra" },
            categories.Select(category => category.Name));
    }

    [Fact]
    public async Task All_WhenNoCategoriesExist_ReturnsEmptyCollection()
    {
        await using var db = CreateContext();

        var result = Assert.IsType<OkObjectResult>(
            await new CategoriesController(db).All());
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value);

        Assert.Empty(categories);
    }

    [Fact]
    public async Task Home_ReturnsOnlyHomeCategoriesSortedAndLimitedToFour()
    {
        await using var db = CreateContext();
        db.Categories.AddRange(
            new Category { Name = "Earrings", ShowOnHomePage = true },
            new Category { Name = "Kurtis", ShowOnHomePage = true },
            new Category { Name = "Sarees", ShowOnHomePage = true },
            new Category { Name = "Tops", ShowOnHomePage = true },
            new Category { Name = "Hidden", ShowOnHomePage = false });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await new CategoriesController(db).Home());
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value);

        Assert.Equal(4, categories.Count());
        Assert.Equal(new[] { "Earrings", "Kurtis", "Sarees", "Tops" },
            categories.Select(category => category.Name));
        Assert.DoesNotContain(categories, category => category.Name == "Hidden");
    }

    [Fact]
    public async Task Home_WhenNoCategoriesAreFeatured_ReturnsEmptyCollection()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Name = "Hidden", ShowOnHomePage = false });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await new CategoriesController(db).Home());
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value);

        Assert.Empty(categories);
    }
}
