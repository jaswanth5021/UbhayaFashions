using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class PaymentsControllerTests {
    [TestCase("CreateOrder")]
    public void CreateOrder_DeclaresHttpVerb(){ var x=typeof(PaymentsController).GetMethod("CreateOrder", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("TestSuccess")]
    public void TestSuccess_DeclaresHttpVerb(){ var x=typeof(PaymentsController).GetMethod("TestSuccess", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Verify")]
    public void Verify_DeclaresHttpVerb(){ var x=typeof(PaymentsController).GetMethod("Verify", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(PaymentsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}