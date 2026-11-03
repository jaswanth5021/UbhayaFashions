using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminDelhiveryControllerTests {
    [Test]
    public void CheckServiceability_DeclaresHttpVerb(){ var x=typeof(AdminDelhiveryController).GetMethod("CheckServiceability", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void CalculateShippingCost_DeclaresHttpVerb(){ var x=typeof(AdminDelhiveryController).GetMethod("CalculateShippingCost", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void FetchWaybills_DeclaresHttpVerb(){ var x=typeof(AdminDelhiveryController).GetMethod("FetchWaybills", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void DownloadLabel_DeclaresHttpVerb(){ var x=typeof(AdminDelhiveryController).GetMethod("DownloadLabel", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void CreateOrRetry_DeclaresHttpVerb(){ var x=typeof(AdminDelhiveryController).GetMethod("CreateOrRetry", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminDelhiveryController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}