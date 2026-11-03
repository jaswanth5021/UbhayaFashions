using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class ContactControllerTests
{
    private static ContactController CreateController()
    {
        var controller = new ContactController();
        ControllerTestHost.Initialize(controller);
        return controller;
    }

    [Test]
    public void IndexGet_ReturnsView()
    {
        Assert.That(CreateController().Index(), Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void IndexPost_InvalidModel_ReturnsSameModel()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Email", "Invalid email");
        var model = new ContactViewModel { Name = "Test" };

        var result = controller.Index(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(((ViewResult)result).Model, Is.SameAs(model));
    }

    [Test]
    public void IndexPost_ValidModel_SetsSuccessAndRedirects()
    {
        var controller = CreateController();
        var model = new ContactViewModel
        {
            Name = "Test Customer",
            Email = "customer@example.com",
            Subject = "Order enquiry",
            Message = "Please contact me about my order."
        };

        var result = controller.Index(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo(nameof(ContactController.Index)));
        Assert.That(controller.TempData["ContactSuccess"], Is.Not.Null);
    }
}
