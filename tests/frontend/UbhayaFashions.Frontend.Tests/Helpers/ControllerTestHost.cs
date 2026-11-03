using System.Net;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace UbhayaFashions.Frontend.Tests.Helpers;

internal static class ControllerTestHost
{
    public static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }

    public static void Initialize(Controller controller, HttpContext? context = null)
    {
        context ??= CreateHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test-generated-url");
        urlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns((string? url) => !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal));
        controller.Url = urlHelper.Object;
        controller.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
    }

    public static ApiService CreateApiService(HttpContext context, HttpStatusCode status = HttpStatusCode.NotFound)
    {
        var handler = new StubHttpMessageHandler(status);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        return new ApiService(factory.Object, new HttpContextAccessor { HttpContext = context });
    }

    public static AdminApiService CreateAdminApiService(HttpContext context, HttpStatusCode status = HttpStatusCode.NotFound)
    {
        var handler = new StubHttpMessageHandler(status);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        return new AdminApiService(factory.Object, new HttpContextAccessor { HttpContext = context });
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("{\"error\":\"test response\"}", System.Text.Encoding.UTF8, "application/json")
            });
    }
}
