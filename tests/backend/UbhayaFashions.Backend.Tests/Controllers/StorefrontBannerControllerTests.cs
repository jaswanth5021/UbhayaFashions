using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class StorefrontBannerControllerTests {
    [TestCase("List")]
    public void List_DeclaresHttpVerb(){ var x=typeof(StorefrontBannerController).GetMethod("List", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Upload")]
    public void Upload_DeclaresHttpVerb(){ var x=typeof(StorefrontBannerController).GetMethod("Upload", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("UpdateStatus")]
    public void UpdateStatus_DeclaresHttpVerb(){ var x=typeof(StorefrontBannerController).GetMethod("UpdateStatus", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Delete")]
    public void Delete_DeclaresHttpVerb(){ var x=typeof(StorefrontBannerController).GetMethod("Delete", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Current")]
    public void Current_DeclaresHttpVerb(){ var x=typeof(StorefrontBannerController).GetMethod("Current", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(StorefrontBannerController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}