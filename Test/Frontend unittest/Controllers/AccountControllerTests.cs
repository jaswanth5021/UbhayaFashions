using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LadiesDressStore.Web.Tests.Controllers;

public sealed class AccountControllerTests
{
    private static AccountController CreateController()
    {
        var api = new ApiService(new TestFactory(), new HttpContextAccessor());
        return new AccountController(api);
    }

    [Fact]
    public void LoginGet_ReturnsExperienceModel()
    {
        var result = Assert.IsType<ViewResult>(CreateController().Login());
        Assert.NotNull(result.Model);
    }

    [Fact]
    public void LoginGet_PreservesReturnUrl()
    {
        var result = Assert.IsType<ViewResult>(CreateController().Login(returnUrl: "/Orders/Checkout"));
        Assert.Equal("/Orders/Checkout", result.ViewData["ReturnUrl"]);
    }

    [Fact]
    public void SignupGet_ReturnsView()
    {
        Assert.IsType<ViewResult>(CreateController().Signup());
    }

    [Fact]
    public void ForgotPasswordGet_ReturnsView()
    {
        Assert.IsType<ViewResult>(CreateController().ForgotPassword());
    }

    [Fact]
    public void ResetPasswordGet_PreservesEmailAndToken()
    {
        var result = Assert.IsType<ViewResult>(
            CreateController().ResetPassword("customer@example.com", "sample-token"));

        Assert.NotNull(result.Model);
    }

    [Fact]
    public void VerifySignupEmailGet_ReturnsView()
    {
        Assert.IsType<ViewResult>(CreateController().VerifySignupEmail("customer@example.com"));
    }

    private sealed class TestFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new HttpClientHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }
}
