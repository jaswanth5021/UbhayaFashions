using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class ProductReviewsControllerTests {
    [TestCase("GetReviews")]
    public void GetReviews_DeclaresHttpVerb(){ var x=typeof(ProductReviewsController).GetMethod("GetReviews", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Vote")]
    public void Vote_DeclaresHttpVerb(){ var x=typeof(ProductReviewsController).GetMethod("Vote", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Create")]
    public void Create_DeclaresHttpVerb(){ var x=typeof(ProductReviewsController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(ProductReviewsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}