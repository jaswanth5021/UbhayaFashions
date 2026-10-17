using backend.Controllers;
using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace UbhayaFashions.Tests.Controllers;

public sealed class AuthControllerTests
{
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"auth-{Guid.NewGuid():N}").Options);

    private static AuthController Create(ApplicationDbContext db) =>
        new(db, new ConfigurationBuilder().Build());

    [Fact]
    public async Task SignUp_MissingNameAndEmail_ReturnsBadRequest()
    {
        await using var db = Db();
        var result = Assert.IsType<BadRequestObjectResult>(
            await Create(db).SignUp(new SignUpRequest { Name = "", Email = "" }));
        Assert.Equal("Name and email are required.", result.Value);
    }

    [Fact]
    public async Task SignUp_ShortPassword_ReturnsBadRequest()
    {
        await using var db = Db();
        var result = Assert.IsType<BadRequestObjectResult>(
            await Create(db).SignUp(new SignUpRequest
            {
                Name = "Customer", Email = "customer@example.com", Password = "12345", Age = 30,
                Mobile = "9876543210"
            }));
        Assert.Equal("Password must contain at least 6 characters.", result.Value);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(121)]
    public async Task SignUp_AgeOutsideAllowedRange_ReturnsBadRequest(int age)
    {
        await using var db = Db();
        var result = Assert.IsType<BadRequestObjectResult>(
            await Create(db).SignUp(new SignUpRequest
            {
                Name = "Customer", Email = "customer@example.com", Password = "password123",
                Age = age, Mobile = "9876543210"
            }));
        Assert.Equal("Age must be between 18 and 120.", result.Value);
    }
}
