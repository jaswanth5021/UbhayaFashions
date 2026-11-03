using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class CategoriesControllerTests {
    [TestCase("All")]
    public void All_DeclaresHttpVerb(){ var x=typeof(CategoriesController).GetMethod("All", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Home")]
    public void Home_DeclaresHttpVerb(){ var x=typeof(CategoriesController).GetMethod("Home", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(CategoriesController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}