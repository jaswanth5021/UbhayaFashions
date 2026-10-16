using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class OrdersControllerTests
{
    [Fact]
    public void Index_RedirectsToAccountOrdersTab()
    {
        var controller = CreateController();

        var result = Assert.IsType<RedirectToActionResult>(controller.Index());

        Assert.Equal("Profile", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
        Assert.Equal("orders", result.RouteValues!["tab"]);
    }

    private static LadiesDressStore.Web.Controllers.OrdersController CreateController()
    {
        var api = new ApiService(new TestFactory(), new HttpContextAccessor());
        return new LadiesDressStore.Web.Controllers.OrdersController(api,
            NullLogger<LadiesDressStore.Web.Controllers.OrdersController>.Instance);
    }

    private sealed class TestFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler())
        { BaseAddress = new Uri("http://localhost/") };
    }
}
