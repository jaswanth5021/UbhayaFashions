using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace UbhayaFashions.Backend.Tests;

[TestFixture]
public class ProductColorCatalogTests
{
    [TestCase(" pink, Pink, navy blue ", "Pink,Navy Blue")]
    [TestCase("red|blue/red", "Red,Blue")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Normalize_CanonicalizesAndRemovesDuplicates(string? input, string expected)
    {
        Assert.That(ProductColorCatalog.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase("Maroon,Gold", "Gold", true)]
    [TestCase("Maroon,Gold", "Red", false)]
    [TestCase("Pink, Beige", "pink", true)]
    [TestCase("Blue,Black", "Black", true)]
    [TestCase("", "Pink", false)]
    [TestCase("Pink", "", true)]
    [TestCase(null, null, true)]
    public void Matches_UsesWholeColorNames(string? stored, string? selected, bool expected)
    {
        Assert.That(ProductColorCatalog.Matches(stored, selected), Is.EqualTo(expected));
    }

    public static IEnumerable<TestCaseData> PriceCeilingCases()
    {
        yield return new TestCaseData(new[] { 3299m, 6499m }, 10000);
        yield return new TestCaseData(new[] { 18450m, 25000m }, 25000);
        yield return new TestCaseData(new[] { 10001m }, 10100);
        yield return new TestCaseData(new[] { 0m, -10m }, 10000);
        yield return new TestCaseData(Array.Empty<decimal>(), 10000);
    }

    [TestCaseSource(nameof(PriceCeilingCases))]
    public void PriceCeiling_HandlesNormalAndBoundaryPrices(decimal[] prices, int expected)
    {
        Assert.That(ProductColorCatalog.PriceCeiling(prices), Is.EqualTo(expected));
    }

    [Test]
    public void PriceCeiling_InvalidStep_UsesFallback()
    {
        Assert.That(ProductColorCatalog.PriceCeiling([1500m], step: 0), Is.EqualTo(10000));
        Assert.That(ProductColorCatalog.PriceCeiling([1500m], step: -1), Is.EqualTo(10000));
    }
}

[TestFixture]
public class ProductsControllerTests
{
    private ApplicationDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"products-tests-{Guid.NewGuid():N}")
            .Options;
        _db = new ApplicationDbContext(options);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Get_UnknownProduct_ReturnsNotFound()
    {
        var controller = new ProductsController(_db);

        var result = await controller.Get(987654);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Get_ExistingProduct_ReturnsProductWithVariants()
    {
        var product = new Product
        {
            Name = "Jaipuri Kurti",
            Description = "Test product",
            CategoryNavigation = new Category { Name = "Kurtis" },
            Colors = "Pink,Blue",
            ImageUrl = "/uploads/test.jpg",
            Variants =
            [
                new ProductVariant { Color = "Pink", Size = "M", Price = 1299m, Stock = 4 }
            ]
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var controller = new ProductsController(_db);
        var result = await controller.Get(product.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var responseProduct = ((OkObjectResult)result).Value as Product;
        Assert.That(responseProduct, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(responseProduct!.Name, Is.EqualTo("Jaipuri Kurti"));
            Assert.That(responseProduct.Variants, Has.Count.EqualTo(1));
            Assert.That(responseProduct.Variants.First().Price, Is.EqualTo(1299m));
        });
    }
}
