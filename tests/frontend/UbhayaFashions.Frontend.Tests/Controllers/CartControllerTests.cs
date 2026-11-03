using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using NUnit.Framework;
using UbhayaFashions.Frontend.Tests.Helpers;

namespace UbhayaFashions.Frontend.Tests.Controllers;

[TestFixture]
public sealed class CartControllerTests
{
    private CartController CreateController()
    {
        var context = ControllerTestHost.CreateHttpContext();
        context.Features.Set<ISessionFeature>(new TestSessionFeature { Session = new EmptySession() });
        var controller = new CartController(ControllerTestHost.CreateApiService(context), new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        ControllerTestHost.Initialize(controller, context);
        return controller;
    }

    [Test]
    public async Task Index_WithEmptySession_ReturnsEmptyCartView()
    {
        var result = await CreateController().Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(((ViewResult)result).Model, Is.InstanceOf<List<CartItemViewModel>>());
        Assert.That(((List<CartItemViewModel>)((ViewResult)result).Model!), Is.Empty);
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class EmptySession : ISession
    {
        public string Id => "test-session";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => Array.Empty<string>();
        public void Clear() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public void Set(string key, byte[] value) { }
        public bool TryGetValue(string key, out byte[]? value) { value = null; return false; }
    }
}
