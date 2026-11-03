using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminDashboardControllerTests {
    [TestCase("Get")]
    public void Get_DeclaresHttpVerb(){ var x=typeof(AdminDashboardController).GetMethod("Get", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminDashboardController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}