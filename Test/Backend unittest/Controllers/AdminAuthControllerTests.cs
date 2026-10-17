using backend.Controllers;
using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class AdminAuthControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"admin-auth-{Guid.NewGuid():N}").Options);

    private static AdminAuthController Create(ApplicationDbContext db) =>
        new(db, new ConfigurationBuilder().Build(), NullLogger<AdminAuthController>.Instance);

    [Fact]
    public async Task Login_MissingEmailAndPassword_ReturnsBadRequest()
    {
        await using var db = Db();
        var result = Assert.IsType<BadRequestObjectResult>(
            await Create(db).Login(new AdminLoginRequest { Email = "", Password = "" }));
        Assert.Equal("Email and password are required.", result.Value);
    }

    [Fact]
    public async Task Login_UnknownAdmin_ReturnsUnauthorized()
    {
        await using var db = Db();
        var result = Assert.IsType<UnauthorizedObjectResult>(
            await Create(db).Login(new AdminLoginRequest
            {
                Email = "missing@example.com", Password = "password123"
            }));
        Assert.Equal("Invalid admin email or password.", result.Value);
    }
}
