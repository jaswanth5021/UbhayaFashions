using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class RecentlyViewedControllerTests {
    [Test]
    public void GetMine_DeclaresHttpVerb(){ var x=typeof(RecentlyViewedController).GetMethod("GetMine", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Record_DeclaresHttpVerb(){ var x=typeof(RecentlyViewedController).GetMethod("Record", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(RecentlyViewedController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}