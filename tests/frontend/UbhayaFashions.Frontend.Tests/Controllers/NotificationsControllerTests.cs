using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class NotificationsControllerTests
{
    private static NotificationsController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var config = new ConfigurationBuilder().Build();
        var controller = new NotificationsController(ControllerTestHost.CreateApiService(context), config);
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public async Task Index_WhenApiUnavailable_ReturnsFallbackView()
    {
        var result = await CreateController().Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(((ViewResult)result).Model, Is.InstanceOf<NotificationListViewModel>());
    }

    [Test]
    public async Task Recent_WhenApiUnavailable_ReturnsBadGateway()
    {
        var result = await CreateController().Recent();
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task Read_WhenApiRejectsRequest_ReturnsJsonResult()
    {
        var result = await CreateController().Read(1);
        Assert.That(result, Is.InstanceOf<JsonResult>());
    }

    [Test]
    public async Task Dismiss_WhenApiRejectsRequest_ReturnsJsonResult()
    {
        var result = await CreateController().Dismiss(1);
        Assert.That(result, Is.InstanceOf<JsonResult>());
    }
}
