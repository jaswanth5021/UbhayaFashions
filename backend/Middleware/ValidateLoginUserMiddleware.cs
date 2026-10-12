using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Middleware;

public class ValidateLoginUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var user = context.User;
        var authenticated = user.Identity?.IsAuthenticated == true;
        var hasAuthorization = context.Request.Headers.ContainsKey("Authorization");

        if (hasAuthorization && !authenticated)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, "Login user is not valid.");
            return;
        }

        if (!authenticated)
        {
            if (IsProtectedAdminApi(context.Request))
            {
                await WriteError(context, StatusCodes.Status401Unauthorized, "Login user is not valid.");
                return;
            }

            await next(context);
            return;
        }

        if (!TryGetUserId(user, out var userId) || !await IsActiveAccountAsync(db, user, userId))
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, "Login user is not valid.");
            return;
        }

        if (!IsAdmin(user) && context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await WriteError(context, StatusCodes.Status403Forbidden, "Customers cannot access admin APIs.");
            return;
        }

        await next(context);
    }

    private static bool IsProtectedAdminApi(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api/admin"))
            return false;

        if (!HttpMethods.IsPost(request.Method))
            return true;

        return !request.Path.StartsWithSegments("/api/admin/auth/login")
            && !request.Path.StartsWithSegments("/api/admin/auth/forgot-password")
            && !request.Path.StartsWithSegments("/api/admin/auth/reset-password");
    }

    private static async Task<bool> IsActiveAccountAsync(ApplicationDbContext db, ClaimsPrincipal user, int userId)
    {
        if (IsAdmin(user))
            return await db.AdminUsers.AsNoTracking().AnyAsync(account => account.Id == userId && account.IsActive);

        return await db.Customers.AsNoTracking().AnyAsync(customer => customer.Id == userId);
    }

    private static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole("Admin")
        || string.Equals(FindClaim(user, ClaimTypes.Role, "role"), "Admin", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId)
    {
        var idText = FindClaim(user, ClaimTypes.NameIdentifier, "nameid", "sub");
        return int.TryParse(idText, out userId);
    }

    private static string? FindClaim(ClaimsPrincipal user, params string[] types)
    {
        foreach (var type in types)
        {
            var value = user.FindFirstValue(type);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(message);
    }
}
