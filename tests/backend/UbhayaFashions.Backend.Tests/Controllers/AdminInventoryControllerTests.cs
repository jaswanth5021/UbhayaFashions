using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminInventoryControllerTests {
    [TestCase("All")]
    public void All_DeclaresHttpVerb(){ var x=typeof(AdminInventoryController).GetMethod("All", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Transactions")]
    public void Transactions_DeclaresHttpVerb(){ var x=typeof(AdminInventoryController).GetMethod("Transactions", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Adjust")]
    public void Adjust_DeclaresHttpVerb(){ var x=typeof(AdminInventoryController).GetMethod("Adjust", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminInventoryController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}