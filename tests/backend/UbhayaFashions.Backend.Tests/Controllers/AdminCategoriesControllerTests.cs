using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminCategoriesControllerTests {
    [TestCase("GetAll")]
    public void GetAll_DeclaresHttpVerb(){ var x=typeof(AdminCategoriesController).GetMethod("GetAll", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Create")]
    public void Create_DeclaresHttpVerb(){ var x=typeof(AdminCategoriesController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Update")]
    public void Update_DeclaresHttpVerb(){ var x=typeof(AdminCategoriesController).GetMethod("Update", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("UploadImage")]
    public void UploadImage_DeclaresHttpVerb(){ var x=typeof(AdminCategoriesController).GetMethod("UploadImage", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Delete")]
    public void Delete_DeclaresHttpVerb(){ var x=typeof(AdminCategoriesController).GetMethod("Delete", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminCategoriesController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}