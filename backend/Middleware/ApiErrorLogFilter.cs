using System.Reflection;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace backend.Middleware;

public sealed class ApiErrorLogFilter(IErrorLog errorLog) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is not null) return;

        var status = executed.Result is IStatusCodeActionResult coded ? coded.StatusCode ?? 200 : 200;
        var message = MessageOf(executed.Result, status);
        if (message is null && !executed.ModelState.IsValid)
            message = string.Join(" ", executed.ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.Ordinal)!);

        if (string.IsNullOrWhiteSpace(message)) return;

        var path = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        var method = context.HttpContext.Request.Method;
        errorLog.Write(
            Actor(context),
            message.Trim().Trim('"'),
            $"HTTP {status} {method} {path}",
            path,
            Area(path));
        context.HttpContext.Items[ErrorLogMiddleware.LoggedItemKey] = true;
    }

    private static string? MessageOf(IActionResult? result, int status)
    {
        var value = result switch
        {
            ObjectResult objectResult => objectResult.Value,
            JsonResult jsonResult => jsonResult.Value,
            ContentResult contentResult => contentResult.Content,
            _ => null
        };

        if (value is string text)
            return status >= 400 ? text : null;

        if (value is ValidationProblemDetails validation)
        {
            var errors = validation.Errors.SelectMany(item => item.Value).Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
            return errors.Length == 0 ? validation.Title : string.Join(" ", errors);
        }

        if (value is ProblemDetails problem)
            return status >= 400 ? problem.Detail ?? problem.Title : null;

        if (value is not null)
        {
            var failed = ReadBool(value, "success") == false || ReadBool(value, "ok") == false;
            var message = ReadString(value, "message") ?? ReadString(value, "error") ?? ReadString(value, "title");
            if (status >= 400 || failed)
                return message ?? $"The request failed with status {status}.";
        }

        return status >= 400 ? $"The request failed with status {status}." : null;
    }

    private static string Actor(ActionExecutingContext context)
    {
        var signedIn = ErrorLogMiddleware.Username(context.HttpContext.User);
        if (!string.Equals(signedIn, "Anonymous", StringComparison.Ordinal)
            && !signedIn.All(char.IsDigit))
            return signedIn;

        string? best = null;
        var bestRank = int.MaxValue;
        foreach (var (name, value) in context.ActionArguments)
        {
            if (value is string text)
            {
                var rank = IdentityRank(name);
                if (rank >= 0 && rank < bestRank && !string.IsNullOrWhiteSpace(text))
                {
                    best = text.Trim();
                    bestRank = rank;
                }
                continue;
            }

            Consider(value, 0, ref best, ref bestRank);
        }

        if (!string.IsNullOrWhiteSpace(best)) return best;
        return string.Equals(signedIn, "Anonymous", StringComparison.Ordinal) ? signedIn : "Anonymous";
    }

    private static void Consider(object? value, int depth, ref string? best, ref int bestRank)
    {
        if (value is null || depth > 3 || value is string) return;

        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead) continue;
            object? current;
            try { current = property.GetValue(value); }
            catch (TargetInvocationException) { continue; }
            if (current is null) continue;

            if (current is string text)
            {
                var rank = IdentityRank(property.Name);
                if (rank >= 0 && rank < bestRank && !string.IsNullOrWhiteSpace(text))
                {
                    best = text.Trim();
                    bestRank = rank;
                }
                continue;
            }

            if (current.GetType().IsClass)
                Consider(current, depth + 1, ref best, ref bestRank);
        }
    }

    private static int IdentityRank(string name)
    {
        if (name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("cvv", StringComparison.OrdinalIgnoreCase)
            || name.Contains("card", StringComparison.OrdinalIgnoreCase)
            || name.Contains("signature", StringComparison.OrdinalIgnoreCase))
            return -1;

        if (name.Contains("email", StringComparison.OrdinalIgnoreCase)) return 0;
        if (name.Contains("identifier", StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.Contains("username", StringComparison.OrdinalIgnoreCase)) return 2;
        if (name.Contains("mobile", StringComparison.OrdinalIgnoreCase) || name.Contains("phone", StringComparison.OrdinalIgnoreCase)) return 3;
        return -1;
    }

    private static bool? ReadBool(object value, string name)
    {
        var property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetValue(value) is bool flag ? flag : null;
    }

    private static string? ReadString(object value, string name)
    {
        var property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        var text = property?.GetValue(value)?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string Area(string path)
    {
        if (path.Contains("/api/payments", StringComparison.OrdinalIgnoreCase)
            || path.Contains("checkout", StringComparison.OrdinalIgnoreCase))
            return "payment";

        if (path.Contains("login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/auth", StringComparison.OrdinalIgnoreCase))
            return "login";

        return "application";
    }
}
