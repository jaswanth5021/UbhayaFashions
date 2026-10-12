using System.Security.Claims;
using backend.Services;

namespace backend.Middleware;

public sealed class ErrorLogMiddleware(RequestDelegate next, IErrorLog errorLog)
{
    public const string LoggedItemKey = "Ubhaya.ErrorLogged";

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var path = context.Request.Path + context.Request.QueryString;
            errorLog.Write(Username(context.User), exception.Message, exception.ToString(), path, Area(path));
            context.Items[LoggedItemKey] = true;
            throw;
        }

        if (context.Items.ContainsKey(LoggedItemKey) || context.Response.StatusCode < 400 || Skip(context.Request.Path))
            return;

        var requestPath = context.Request.Path + context.Request.QueryString;
        var status = context.Response.StatusCode;
        errorLog.Write(
            Username(context.User),
            Fallback(status),
            $"HTTP {status} {context.Request.Method} {requestPath}",
            requestPath,
            Area(requestPath));
    }

    private static bool Skip(PathString path)
    {
        var value = path.Value ?? "";
        if (value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) || value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            return true;

        var extension = Path.GetExtension(value);
        return extension is ".css" or ".js" or ".map" or ".png" or ".jpg" or ".jpeg" or ".svg" or ".ico" or ".webp" or ".woff" or ".woff2";
    }

    private static string Fallback(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => "Sign-in is required or the login is not valid.",
        StatusCodes.Status403Forbidden => "Access was denied.",
        StatusCodes.Status404NotFound => "The page or record was not found.",
        _ => $"The request failed with status {status}."
    };

    private static string Area(string path)
    {
        if (path.Contains("/api/payments", StringComparison.OrdinalIgnoreCase) || path.Contains("checkout", StringComparison.OrdinalIgnoreCase))
            return "payment";

        if (path.Contains("login", StringComparison.OrdinalIgnoreCase) || path.Contains("/auth", StringComparison.OrdinalIgnoreCase))
            return "login";

        return "application";
    }

    internal static string Username(ClaimsPrincipal user)
    {
        return user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "Anonymous";
    }
}
