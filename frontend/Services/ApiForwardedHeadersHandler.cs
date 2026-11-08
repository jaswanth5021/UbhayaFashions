namespace LadiesDressStore.Web.Services;

// The API must trust only this frontend's network address. Never copy browser-supplied forwarding headers.
public sealed class ApiForwardedHeadersHandler(IHttpContextAccessor accessor, IHostEnvironment environment) : DelegatingHandler
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
            var forwardedProto = environment.IsDevelopment()
                && string.Equals(request.RequestUri?.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    ? Uri.UriSchemeHttps
                    : context.Request.Scheme;
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", forwardedProto);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
