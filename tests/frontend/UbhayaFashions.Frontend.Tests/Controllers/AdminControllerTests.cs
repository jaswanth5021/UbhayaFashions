using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class AdminControllerTests
{
    private AdminController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new AdminController(ControllerTestHost.CreateAdminApiService(context), Mock.Of<IErrorLog>());
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public void Index_RedirectsToLogin()
    {
        var result = CreateController().Index();
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo("Login"));
    }

    [Test]
    public void Login_WithoutAdminCookie_ReturnsView()
    {
        Assert.That(CreateController().Login(), Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void ChangePassword_WithoutAdminCookie_RedirectsToLogin()
    {
        var result = CreateController().ChangePassword();
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo("Login"));
    }

    [Test]
    public void ForgotPasswordGet_ReturnsView()
    {
        Assert.That(CreateController().ForgotPassword(), Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void ResetPasswordGet_PopulatesEmailAndToken()
    {
        var result = CreateController().ResetPassword("admin@example.com", "token");
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var model = (LadiesDressStore.Web.Models.AdminResetPasswordViewModel)((ViewResult)result).Model!;
        Assert.That(model.Email, Is.EqualTo("admin@example.com"));
        Assert.That(model.Token, Is.EqualTo("token"));
    }
}
