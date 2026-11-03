using LadiesDressStore.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class ShippingControllerTests
{
    [Test]
    public void Index_ReturnsView()
    {
        var controller = new ShippingController();
        ControllerTestHost.Initialize(controller);
        Assert.That(controller.Index(), Is.InstanceOf<ViewResult>());
    }
}
