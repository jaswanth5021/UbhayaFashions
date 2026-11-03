using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminOrdersControllerTests {
    [TestCase("All")]
    public void All_DeclaresHttpVerb(){ var x=typeof(AdminOrdersController).GetMethod("All", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Get")]
    public void Get_DeclaresHttpVerb(){ var x=typeof(AdminOrdersController).GetMethod("Get", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Status")]
    public void Status_DeclaresHttpVerb(){ var x=typeof(AdminOrdersController).GetMethod("Status", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminOrdersController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}