using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class OrdersControllerTests {
    [TestCase("Create")]
    public void Create_DeclaresHttpVerb(){ var x=typeof(OrdersController).GetMethod("Create", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Cancel")]
    public void Cancel_DeclaresHttpVerb(){ var x=typeof(OrdersController).GetMethod("Cancel", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("MyOrders")]
    public void MyOrders_DeclaresHttpVerb(){ var x=typeof(OrdersController).GetMethod("MyOrders", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("TrackOrder")]
    public void TrackOrder_DeclaresHttpVerb(){ var x=typeof(OrdersController).GetMethod("TrackOrder", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(OrdersController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}