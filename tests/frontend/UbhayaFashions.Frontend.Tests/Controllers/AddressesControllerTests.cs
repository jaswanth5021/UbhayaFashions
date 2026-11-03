using LadiesDressStore.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class AddressesControllerTests
{
    private AddressesController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var factory = new HttpClientFactoryStub();
        var controller = new AddressesController(ControllerTestHost.CreateApiService(context), factory);
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [TestCase(-91, 0)]
    [TestCase(91, 0)]
    [TestCase(0, -181)]
    [TestCase(0, 181)]
    public async Task ReverseGeocode_InvalidCoordinates_ReturnsBadRequest(double latitude, double longitude)
    {
        var result = await CreateController().ReverseGeocode(latitude, longitude);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    private sealed class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler());
        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }
    }

    [Test]
    public async Task ReverseGeocode_ValidCoordinates_WhenProviderUnavailable_ReturnsBadRequest()
    {
        var result = await CreateController().ReverseGeocode(17.385, 78.4867);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
