using backend.Controllers;
using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class WishlistControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"wishlist-{Guid.NewGuid():N}").Options);

    [Fact] public async Task GetMine_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new WishlistController(db).GetMine()); }

    [Fact] public async Task Add_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new WishlistController(db).Add(1)); }

    [Fact] public async Task Remove_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new WishlistController(db).Remove(1)); }

    [Fact] public async Task Add_UnknownProduct_ReturnsNotFoundForAuthenticatedCustomer()
    {
        await using var db = Db();
        var controller = new WishlistController(db);
        controller.ControllerContext = TestControllerContext.ForCustomer(42);
        Assert.IsType<NotFoundResult>(await controller.Add(999));
    }
}
