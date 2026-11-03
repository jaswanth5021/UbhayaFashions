using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminAuthControllerTests {
    [Test]
    public void Login_DeclaresHttpVerb(){ var x=typeof(AdminAuthController).GetMethod("Login", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void ChangePassword_DeclaresHttpVerb(){ var x=typeof(AdminAuthController).GetMethod("ChangePassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void ForgotPassword_DeclaresHttpVerb(){ var x=typeof(AdminAuthController).GetMethod("ForgotPassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void ResetPassword_DeclaresHttpVerb(){ var x=typeof(AdminAuthController).GetMethod("ResetPassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminAuthController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}