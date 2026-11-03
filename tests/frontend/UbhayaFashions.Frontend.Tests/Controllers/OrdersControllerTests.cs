using LadiesDressStore.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class OrdersControllerTests
{
    private static OrdersController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new OrdersController(ControllerTestHost.CreateApiService(context), NullLogger<OrdersController>.Instance);
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public void Index_RedirectsToAccountOrderTab()
    {
        var result = CreateController().Index();
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = (RedirectToActionResult)result;
        Assert.Multiple(() =>
        {
            Assert.That(redirect.ActionName, Is.EqualTo("Profile"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Account"));
            Assert.That(redirect.RouteValues!["tab"], Is.EqualTo("orders"));
        });
    }

    [Test]
    public void Success_ExposesOrderId()
    {
        var controller = CreateController();
        var result = controller.Success(42);
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(controller.ViewBag.OrderId, Is.EqualTo(42));
    }

    [Test]
    public void Failure_WithoutMessage_UsesSafeDefaultMessage()
    {
        var controller = CreateController();
        var result = controller.Failure(42, null);
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.Multiple(() =>
        {
            Assert.That(controller.ViewBag.OrderId, Is.EqualTo(42));
            Assert.That(controller.ViewBag.FailureMessage, Is.EqualTo("Your payment was not completed."));
        });
    }

    [Test]
    public void Failure_WithMessage_PreservesProvidedMessage()
    {
        var controller = CreateController();
        controller.Failure(null, "Payment declined");
        Assert.That(controller.ViewBag.FailureMessage, Is.EqualTo("Payment declined"));
    }
}
