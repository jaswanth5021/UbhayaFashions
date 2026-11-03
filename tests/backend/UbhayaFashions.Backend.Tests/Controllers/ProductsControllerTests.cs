using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class ProductsControllerTests {
    [TestCase("GetAll")]
    public void GetAll_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetAll", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetColors")]
    public void GetColors_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetColors", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetColorCounts")]
    public void GetColorCounts_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetColorCounts", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetPriceBounds")]
    public void GetPriceBounds_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetPriceBounds", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Get")]
    public void Get_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("Get", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetRelated")]
    public void GetRelated_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetRelated", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("GetNewarrivals")]
    public void GetNewarrivals_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("GetNewarrivals", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Create")]
    public void Create_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Update")]
    public void Update_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("Update", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Delete")]
    public void Delete_DeclaresHttpVerb(){ var x=typeof(ProductsController).GetMethod("Delete", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(ProductsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}