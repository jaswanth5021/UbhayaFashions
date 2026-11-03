using System.Reflection; using backend.Controllers; using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing; using NUnit.Framework;
namespace UbhayaFashions.Backend.Tests.Controllers;
[TestFixture] public sealed class NotificationsControllerTests {
    [TestCase("Mine")]
    public void Mine_DeclaresHttpVerb(){ var x=typeof(NotificationsController).GetMethod("Mine", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("MarkRead")]
    public void MarkRead_DeclaresHttpVerb(){ var x=typeof(NotificationsController).GetMethod("MarkRead", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
    [TestCase("Dismiss")]
    public void Dismiss_DeclaresHttpVerb(){ var x=typeof(NotificationsController).GetMethod("Dismiss", BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly); Assert.That(x,Is.Not.Null); Assert.That(x!.GetCustomAttributes<HttpMethodAttribute>(true),Is.Not.Empty); }
[Test] public void Controller_HasRoute(){Assert.That(typeof(NotificationsController).GetCustomAttribute<RouteAttribute>(),Is.Not.Null);}
}