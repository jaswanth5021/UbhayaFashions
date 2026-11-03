using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AuthControllerTests {
    [TestCase("SignUp")]
    public void SignUp_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("SignUp", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("SendMyEmailVerification")]
    public void SendMyEmailVerification_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("SendMyEmailVerification", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ConfirmMyEmailVerification")]
    public void ConfirmMyEmailVerification_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("ConfirmMyEmailVerification", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ResendSignupOtp")]
    public void ResendSignupOtp_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("ResendSignupOtp", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("VerifySignupEmail")]
    public void VerifySignupEmail_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("VerifySignupEmail", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Login")]
    public void Login_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("Login", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetMyProfile")]
    public void GetMyProfile_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("GetMyProfile", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("UpdateMyProfile")]
    public void UpdateMyProfile_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("UpdateMyProfile", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ChangePassword")]
    public void ChangePassword_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("ChangePassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("DeleteMyAccount")]
    public void DeleteMyAccount_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("DeleteMyAccount", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ForgotPassword")]
    public void ForgotPassword_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("ForgotPassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ResetPassword")]
    public void ResetPassword_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("ResetPassword", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Google")]
    public void Google_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("Google", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Facebook")]
    public void Facebook_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("Facebook", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GoogleResponse")]
    public void GoogleResponse_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("GoogleResponse", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("FacebookResponse")]
    public void FacebookResponse_DeclaresHttpVerb(){ var x=typeof(AuthController).GetMethod("FacebookResponse", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AuthController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}