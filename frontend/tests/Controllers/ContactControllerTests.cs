using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class ContactControllerTests
{
    [Fact]
    public void GetIndex_ReturnsView()
    {
        var result = new ContactController().Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void PostIndex_InvalidModel_ReturnsSameViewAndModel()
    {
        var controller = new ContactController();
        controller.ModelState.AddModelError(nameof(ContactViewModel.Email), "Invalid email");
        var model = new ContactViewModel { Name = "Test", Email = "bad", Subject = "Question", Message = "Hello" };

        var result = Assert.IsType<ViewResult>(controller.Index(model));

        Assert.Same(model, result.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Null(controller.TempData["ContactSuccess"]);
    }

    [Fact]
    public void PostIndex_ValidModel_RedirectsAndSetsSuccessMessage()
    {
        var controller = new ContactController();
        var model = new ContactViewModel
        {
            Name = "Test Customer",
            Email = "customer@example.com",
            Subject = "Order question",
            Message = "I have a question about my order."
        };

        var result = Assert.IsType<RedirectToActionResult>(controller.Index(model));

        Assert.Equal(nameof(ContactController.Index), result.ActionName);
        Assert.Equal("Thank you for contacting Ubhaya Fashions. We will get back to you soon.",
            controller.TempData["ContactSuccess"]);
    }
}
