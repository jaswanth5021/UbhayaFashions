using backend.Controllers;
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class OrdersControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"orders-{Guid.NewGuid():N}").Options);

    [Fact] public async Task Create_WithoutCustomerIdentity_ReturnsUnauthorized()
    {
        await using var db = Db();
        var email = new Mock<IOrderConfirmationEmailService>();
        Assert.IsType<UnauthorizedResult>(await new OrdersController(db, email.Object).Create(null!));
    }
}
