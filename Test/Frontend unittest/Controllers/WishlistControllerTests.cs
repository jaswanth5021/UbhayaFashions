using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class WishlistControllerTests
{
    [Fact]
    public async Task Index_AnonymousVisitor_IsRedirectedToLogin()
    {
        var api = new ApiService(new TestFactory(), new HttpContextAccessor());
        var controller = new WishlistController(api);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Url = new TestUrlHelper();

        var result = Assert.IsType<RedirectToActionResult>(await controller.Index());

        Assert.Equal("Login", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }

    private sealed class TestFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler())
        { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext actionContext) => "/Wishlist";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true;
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => "/Wishlist";
    }
}
