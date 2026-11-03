using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class HomeControllerTests
{
    private static HomeController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new HomeController(ControllerTestHost.CreateApiService(context), NullLogger<HomeController>.Instance);
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [TestCase("Privacy")]
    [TestCase("Terms")]
    [TestCase("Returns")]
    [TestCase("Cancellation")]
    [TestCase("About")]
    public void StaticInformationPages_ReturnView(string action)
    {
        var controller = CreateController();
        var result = action switch
        {
            "Privacy" => controller.Privacy(),
            "Terms" => controller.Terms(),
            "Returns" => controller.Returns(),
            "Cancellation" => controller.Cancellation(),
            _ => controller.About()
        };
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void NotFoundPage_Sets404AndUsesNotFoundView()
    {
        var controller = CreateController();
        var result = controller.NotFoundPage();
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.Response.StatusCode, Is.EqualTo(404));
        Assert.That(((ViewResult)result).ViewName, Is.EqualTo("NotFound"));
    }

    [Test]
    public void Error_Sets500AndReturnsView()
    {
        var controller = CreateController();
        var result = controller.Error();
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.Response.StatusCode, Is.EqualTo(500));
    }
}
