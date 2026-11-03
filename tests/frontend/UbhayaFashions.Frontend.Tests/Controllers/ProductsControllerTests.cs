using LadiesDressStore.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class ProductsControllerTests
{
    private static ProductsController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new ProductsController(ControllerTestHost.CreateApiService(context), NullLogger<ProductsController>.Instance);
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task Details_InvalidProductId_ReturnsNotFound(int id)
    {
        var result = await CreateController().Details(id);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
