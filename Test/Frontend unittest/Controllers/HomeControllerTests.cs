using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class HomeControllerTests
{
    private static HomeController CreateController()
    {
        var factory = new TestHttpClientFactory();
        var api = new ApiService(factory, new HttpContextAccessor());
        return new HomeController(api, NullLogger<HomeController>.Instance);
    }

    [Theory]
    [InlineData("Privacy")]
    [InlineData("Terms")]
    [InlineData("Returns")]
    [InlineData("About")]
    public void InformationalPages_ReturnView(string action)
    {
        var controller = CreateController();
        var result = action switch
        {
            "Privacy" => controller.Privacy(),
            "Terms" => controller.Terms(),
            "Returns" => controller.Returns(),
            _ => controller.About()
        };

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void NotFoundPage_Sets404AndUsesNotFoundView()
    {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = Assert.IsType<ViewResult>(controller.NotFoundPage());

        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
        Assert.Equal("NotFound", result.ViewName);
    }

    [Fact]
    public void Error_Sets500AndReturnsView()
    {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        Assert.IsType<ViewResult>(controller.Error());
        Assert.Equal(StatusCodes.Status500InternalServerError, controller.Response.StatusCode);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }
}
