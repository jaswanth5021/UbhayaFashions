using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace UbhayaFashions.Backend.Tests.Controllers;

[TestFixture]
public sealed class ProductsControllerTests
{
    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"products-controller-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Product CreateProduct(string name, string colors, decimal price, int stock = 3)
    {
        var category = new Category { Name = "Kurtis" };
        return new Product
        {
            Name = name,
            Description = "Automated test product",
            CategoryNavigation = category,
            Colors = colors,
            ImageUrl = "/uploads/test.jpg",
            Variants =
            [
                new ProductVariant { Color = ProductColorCatalog.Split(colors).FirstOrDefault() ?? "", Size = "M", Price = price, Stock = stock }
            ]
        };
    }

    [Test]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var result = await new ProductsController(db).Get(99999);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Get_ExistingProduct_ReturnsProductAndVariant()
    {
        await using var db = CreateDb();
        var product = CreateProduct("Jaipuri Kurti", "Pink,Blue", 1299m);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await new ProductsController(db).Get(product.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var value = (Product)((OkObjectResult)result).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(value.Name, Is.EqualTo("Jaipuri Kurti"));
            Assert.That(value.Variants, Has.Count.EqualTo(1));
            Assert.That(value.Variants.First().Price, Is.EqualTo(1299m));
        });
    }

    [Test]
    public async Task GetColors_ReturnsDistinctNormalizedColors()
    {
        await using var db = CreateDb();
        db.Products.AddRange(
            CreateProduct("Product One", "pink,Blue", 1000m),
            CreateProduct("Product Two", "Pink,Gold", 1200m));
        await db.SaveChangesAsync();

        var result = await new ProductsController(db).GetColors();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var colors = ((OkObjectResult)result).Value as List<string>;
        Assert.That(colors, Is.Not.Null);
        Assert.That(colors!.Count(x => x.Equals("Pink", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
        Assert.That(colors, Does.Contain("Blue"));
        Assert.That(colors, Does.Contain("Gold"));
    }

    [Test]
    public async Task GetColorCounts_CountsEachColorOncePerProduct()
    {
        await using var db = CreateDb();
        db.Products.AddRange(
            CreateProduct("Product One", "Pink,pink,Blue", 1000m),
            CreateProduct("Product Two", "Pink,Gold", 1200m));
        await db.SaveChangesAsync();

        var result = await new ProductsController(db).GetColorCounts();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var counts = (Dictionary<string, int>)((OkObjectResult)result).Value!;
        Assert.That(counts["Pink"], Is.EqualTo(2));
        Assert.That(counts["Blue"], Is.EqualTo(1));
        Assert.That(counts["Gold"], Is.EqualTo(1));
    }

    [Test]
    public async Task GetPriceBounds_UsesConfiguredFallbackWhenNoVariantsExist()
    {
        await using var db = CreateDb();
        var result = await new ProductsController(db).GetPriceBounds();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var value = ((OkObjectResult)result).Value!;
        Assert.That(value.GetType().GetProperty("max")!.GetValue(value), Is.EqualTo(10000));
    }

    [Test]
    public async Task GetRelated_UnknownProduct_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var result = await new ProductsController(db).GetRelated(12345);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
