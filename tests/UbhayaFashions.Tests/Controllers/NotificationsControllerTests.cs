using backend.Controllers;
using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class NotificationsControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"notifications-{Guid.NewGuid():N}").Options);

    [Fact] public async Task Mine_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new NotificationsController(db).Mine()); }

    [Fact] public async Task MarkRead_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new NotificationsController(db).MarkRead(1)); }

    [Fact] public async Task Dismiss_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new NotificationsController(db).Dismiss(1)); }

    [Fact] public async Task MarkRead_UnknownNotification_ReturnsNotFound()
    {
        await using var db = Db();
        var controller = new NotificationsController(db);
        controller.ControllerContext = TestControllerContext.ForCustomer(42);
        Assert.IsType<NotFoundResult>(await controller.MarkRead(999));
    }

    [Fact] public async Task Dismiss_UnknownNotification_ReturnsNotFound()
    {
        await using var db = Db();
        var controller = new NotificationsController(db);
        controller.ControllerContext = TestControllerContext.ForCustomer(42);
        Assert.IsType<NotFoundResult>(await controller.Dismiss(999));
    }
}
