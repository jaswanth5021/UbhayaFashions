using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class AccountControllerTests
{
    private AccountController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        var controller = new AccountController(ControllerTestHost.CreateApiService(context));
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public void LoginGet_ReturnsLoginExperienceViewModel()
    {
        var result = CreateController().Login("sample error", "/Orders/Checkout");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(((ViewResult)result).Model, Is.InstanceOf<LoginExperienceViewModel>());
    }

    [Test]
    public void SignupGet_ReturnsView()
    {
        var result = CreateController().Signup();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void ForgotPasswordGet_ReturnsViewModel()
    {
        var result = CreateController().ForgotPassword();
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(((ViewResult)result).Model, Is.InstanceOf<ForgotPasswordViewModel>());
    }

    [Test]
    public void ResetPasswordGet_PreservesEmailAndToken()
    {
        var result = CreateController().ResetPassword("customer@example.com", "reset-token");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var model = (ResetPasswordViewModel)((ViewResult)result).Model!;
        Assert.Multiple(() =>
        {
            Assert.That(model.Email, Is.EqualTo("customer@example.com"));
            Assert.That(model.Token, Is.EqualTo("reset-token"));
        });
    }

    [Test]
    public void VerifySignupEmailGet_PreservesEmail()
    {
        var result = CreateController().VerifySignupEmail("customer@example.com");
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }
}
