using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class AdminProductsControllerTests {
    [TestCase("All")]
    public void All_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("All", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Get")]
    public void Get_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Get", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Create")]
    public void Create_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Update")]
    public void Update_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Update", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("SetBestSeller")]
    public void SetBestSeller_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("SetBestSeller", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Delete")]
    public void Delete_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("Delete", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("MainImage")]
    public void MainImage_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("MainImage", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("AdditionalImages")]
    public void AdditionalImages_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("AdditionalImages", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("DeleteAdditionalImage")]
    public void DeleteAdditionalImage_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("DeleteAdditionalImage", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("ProductVideos")]
    public void ProductVideos_DeclaresHttpVerb(){ var x=typeof(AdminProductsController).GetMethod("ProductVideos", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(AdminProductsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}