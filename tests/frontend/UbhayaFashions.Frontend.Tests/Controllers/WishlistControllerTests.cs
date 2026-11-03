using LadiesDressStore.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class WishlistControllerTests
{
    private static WishlistController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new WishlistController(ControllerTestHost.CreateApiService(context));
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public async Task Index_AnonymousUser_RedirectsToLogin()
    {
        var result = await CreateController().Index();
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = (RedirectToActionResult)result;
        Assert.That(redirect.ActionName, Is.EqualTo("Login"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Account"));
    }

    [Test]
    public async Task Toggle_AnonymousAjaxRequest_ReturnsUnauthorized()
    {
        var controller = CreateController();
        controller.HttpContext.Request.Headers.XRequestedWith = "XMLHttpRequest";

        var result = await controller.Toggle(10, false, null);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Toggle_AnonymousRegularRequest_RedirectsToLogin()
    {
        var result = await CreateController().Toggle(10, false, null);
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo("Login"));
    }
}
