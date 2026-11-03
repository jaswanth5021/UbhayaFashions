using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace UbhayaFashions.Backend.Tests.Controllers;

[TestFixture]
public sealed class CategoriesControllerTests
{
    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"categories-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task All_ReturnsAllCategoriesSortedByName()
    {
        await using var db = CreateDb();
        db.Categories.AddRange(
            new Category { Name = "Z Kurtis" },
            new Category { Name = "A Sarees" });
        await db.SaveChangesAsync();

        var result = await new CategoriesController(db).All();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var values = ((OkObjectResult)result).Value as List<Category>;
        Assert.That(values!.Select(x => x.Name), Is.EqualTo(new[] { "A Sarees", "Z Kurtis" }));
    }

    [Test]
    public async Task All_WhenDatabaseIsEmpty_ReturnsEmptyCollection()
    {
        await using var db = CreateDb();
        var result = await new CategoriesController(db).All();
        Assert.That(((OkObjectResult)result).Value, Is.Empty);
    }

    [Test]
    public async Task Home_ReturnsOnlyHomeCategoriesAndLimitsToFour()
    {
        await using var db = CreateDb();
        db.Categories.AddRange(
            new Category { Name = "E", ShowOnHomePage = true },
            new Category { Name = "D", ShowOnHomePage = true },
            new Category { Name = "C", ShowOnHomePage = true },
            new Category { Name = "B", ShowOnHomePage = true },
            new Category { Name = "A", ShowOnHomePage = true },
            new Category { Name = "Hidden", ShowOnHomePage = false });
        await db.SaveChangesAsync();

        var result = await new CategoriesController(db).Home();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var values = ((OkObjectResult)result).Value as List<Category>;
        Assert.That(values, Has.Count.EqualTo(4));
        Assert.That(values!.Select(x => x.Name), Is.EqualTo(new[] { "A", "B", "C", "D" }));
        Assert.That(values.Any(x => x.Name == "Hidden"), Is.False);
    }
}
