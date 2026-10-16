using backend.Controllers;
using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class RecentlyViewedControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"recently-viewed-{Guid.NewGuid():N}").Options);

    [Fact] public async Task GetMine_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new RecentlyViewedController(db).GetMine()); }

    [Fact] public async Task Record_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new RecentlyViewedController(db).Record(1)); }

    [Fact] public async Task Record_UnknownProduct_ReturnsNotFound()
    {
        await using var db = Db();
        var controller = new RecentlyViewedController(db);
        controller.ControllerContext = TestControllerContext.ForCustomer(42);
        Assert.IsType<NotFoundResult>(await controller.Record(999));
    }

    [Fact] public async Task GetMine_ForCustomerWithNoHistory_ReturnsEmptyCollection()
    {
        await using var db = Db();
        var controller = new RecentlyViewedController(db);
        controller.ControllerContext = TestControllerContext.ForCustomer(42);
        var result = Assert.IsType<OkObjectResult>(await controller.GetMine());
        Assert.Empty(Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Value).Cast<object>());
    }
}
