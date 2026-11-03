using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class ClientLogControllerTests
{
    [Test]
    public void Create_EmptyMessage_ReturnsNoContentWithoutLogging()
    {
        var log = new Mock<IErrorLog>();
        var controller = new ClientLogController(log.Object);
        ControllerTestHost.Initialize(controller);

        var result = controller.Create("  ", "stack", "/checkout");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        log.Verify(x => x.Write(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Create_NonEmptyMessage_LogsPaymentAreaAndReturnsNoContent()
    {
        var log = new Mock<IErrorLog>();
        var controller = new ClientLogController(log.Object);
        ControllerTestHost.Initialize(controller);

        var result = controller.Create(" Payment failed ", " detail ", "/checkout/payment");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        log.Verify(x => x.Write(It.IsAny<string?>(), "Payment failed", "detail", "/checkout/payment", "payment"), Times.Once);
    }

    [Test]
    public void Create_OrdinaryPage_UsesBrowserArea()
    {
        var log = new Mock<IErrorLog>();
        var controller = new ClientLogController(log.Object);
        ControllerTestHost.Initialize(controller);

        controller.Create("Unhandled error", null, "/products");

        log.Verify(x => x.Write(It.IsAny<string?>(), "Unhandled error", "", "/products", "browser"), Times.Once);
    }
}
