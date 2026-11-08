namespace LadiesDressStore.Web.Services;

// The API must trust only this frontend's network address. Never copy browser-supplied forwarding headers.
public sealed class ApiForwardedHeadersHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        if (context is not null)
        {
            request.Headers.Remove("X-Forwarded-For");
            request.Headers.Remove("X-Forwarded-Proto");
            if (context.Connection.RemoteIpAddress is { } address)
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", address.ToString());
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
