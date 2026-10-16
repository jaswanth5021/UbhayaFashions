using backend.Controllers;
using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class AddressesControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"addresses-{Guid.NewGuid():N}").Options);

    [Fact] public async Task GetMine_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new AddressesController(db).GetMine()); }

    [Fact] public async Task Create_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new AddressesController(db).Create(null!)); }

    [Fact] public async Task Update_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new AddressesController(db).Update(1, null!)); }

    [Fact] public async Task Delete_WithoutCustomerIdentity_ReturnsUnauthorized()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new AddressesController(db).Delete(1)); }

    [Fact] public async Task Update_UnknownAddress_ReturnsUnauthorizedBeforeLookup()
    { await using var db = Db(); Assert.IsType<UnauthorizedResult>(await new AddressesController(db).Update(999, null!)); }
}
