using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class WishlistControllerTests {
    [Test]
    public void GetMine_DeclaresHttpVerb(){ var x=typeof(WishlistController).GetMethod("GetMine", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Add_DeclaresHttpVerb(){ var x=typeof(WishlistController).GetMethod("Add", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Remove_DeclaresHttpVerb(){ var x=typeof(WishlistController).GetMethod("Remove", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(WishlistController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}