using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminProductsControllerTests {
    [Test]
    public void All_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("All", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Get_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Get", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Create_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Update_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Update", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void SetBestSeller_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("SetBestSeller", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void Delete_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Delete", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void MainImage_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("MainImage", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void AdditionalImages_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("AdditionalImages", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void DeleteAdditionalImage_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("DeleteAdditionalImage", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [Test]
    public void ProductVideos_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("ProductVideos", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminProductsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}