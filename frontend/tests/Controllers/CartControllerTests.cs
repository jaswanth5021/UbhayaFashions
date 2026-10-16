using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class CartControllerTests
{
    [Fact]
    public async Task Index_EmptyCart_ReturnsViewWithEmptyItems()
    {
        var api = new ApiService(new TestFactory(), new HttpContextAccessor());
        var controller = new CartController(api, new MemoryCache(new MemoryCacheOptions()));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = Assert.IsType<ViewResult>(await controller.Index());

        Assert.NotNull(result.Model);
        Assert.Empty(Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Model).Cast<object>());
    }

    private sealed class TestFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler())
        { BaseAddress = new Uri("http://localhost/") };
    }
}
