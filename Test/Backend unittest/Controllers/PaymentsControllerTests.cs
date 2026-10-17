using backend.Controllers;
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class PaymentsControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"payments-{Guid.NewGuid():N}").Options);

    private static PaymentsController CreateController(ApplicationDbContext db) =>
        new(db, new ConfigurationBuilder().Build(), Mock.Of<IHttpClientFactory>(),
            Mock.Of<IOrderConfirmationEmailService>());

    [Fact] public async Task CreateOrder_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await CreateController(db).CreateOrder(null!)); }

    [Fact] public async Task TestSuccess_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await CreateController(db).TestSuccess(null!)); }

    [Fact] public async Task Verify_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await CreateController(db).Verify(null!)); }
}
