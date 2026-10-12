using System.Reflection;
using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LadiesDressStore.Web.Middleware;

public sealed class UserErrorLogFilter(IErrorLog errorLog) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is not null) return;
        if (context.Controller is ClientLogController) return;

        var messages = new List<string>();
        CollectResultMessage(executed.Result, messages);

        var method = context.HttpContext.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
        {
            CollectModelErrors(executed.ModelState, messages);
            CollectStoredErrors(executed.Controller as Controller, messages);
        }
        else if (executed.Controller is Controller controller)
        {
            AddMessage(messages, controller.ViewData["Error"]?.ToString());
        }

        if (messages.Count == 0) return;

        var path = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        errorLog.Write(
            Actor(context),
            string.Join(" ", messages.Distinct(StringComparer.Ordinal)),
            $"{method} {path}",
            path,
            Area(path));
        context.HttpContext.Items[ErrorLogMiddleware.LoggedItemKey] = true;
    }

    private static void CollectResultMessage(IActionResult? result, List<string> messages)
    {
        if (result is ForbidResult)
        {
            AddMessage(messages, "Access was denied.");
            return;
        }

        if (result is ChallengeResult) return;

        var status = result is IStatusCodeActionResult coded ? coded.StatusCode ?? 200 : 200;
        var value = result switch
        {
            ObjectResult objectResult => objectResult.Value,
            JsonResult jsonResult => jsonResult.Value,
            ContentResult contentResult => contentResult.Content,
            _ => null
        };

        if (value is string text)
        {
            if (status >= 400) AddMessage(messages, text);
            return;
        }

        if (value is ValidationProblemDetails validation)
        {
            foreach (var error in validation.Errors.SelectMany(item => item.Value))
                AddMessage(messages, error);
            return;
        }

        if (value is ProblemDetails problem)
        {
            AddMessage(messages, problem.Detail ?? problem.Title);
            return;
        }

        if (value is null)
        {
            if (status >= 400) AddMessage(messages, Fallback(status));
            return;
        }

        var failed = ReadBool(value, "success") == false || ReadBool(value, "ok") == false;
        var message = ReadString(value, "message") ?? ReadString(value, "error") ?? ReadString(value, "title");
        if (status >= 400 || failed)
            AddMessage(messages, message ?? Fallback(status));
    }

    private static void CollectModelErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, List<string> messages)
    {
        if (modelState.IsValid) return;
        foreach (var entry in modelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                var text = string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage;
                AddMessage(messages, text);
            }
        }
    }

    private static void CollectStoredErrors(Controller? controller, List<string> messages)
    {
        if (controller is null) return;

        foreach (var item in controller.ViewData)
        {
            if (item.Key.Contains("Error", StringComparison.OrdinalIgnoreCase))
                AddMessage(messages, item.Value?.ToString());
        }

        foreach (var key in controller.TempData.Keys)
        {
            if (!key.Contains("Error", StringComparison.OrdinalIgnoreCase)) continue;
            AddMessage(messages, controller.TempData.Peek(key)?.ToString());
        }
    }

    private static string Actor(ActionExecutingContext context)
    {
        var signedIn = ErrorLogMiddleware.Username(context.HttpContext.User);
        if (!string.Equals(signedIn, "Anonymous", StringComparison.Ordinal)) return signedIn;

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

        return string.IsNullOrWhiteSpace(best) ? "Anonymous" : best;
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

            if (current.GetType().IsClass && current is not IFormFile)
                Consider(current, depth + 1, ref best, ref bestRank);
        }
    }

    internal static int IdentityRank(string name)
    {
        if (name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("cvv", StringComparison.OrdinalIgnoreCase)
            || name.Contains("card", StringComparison.OrdinalIgnoreCase)
            || name.Contains("signature", StringComparison.OrdinalIgnoreCase)
            || name.Equals("code", StringComparison.OrdinalIgnoreCase))
            return -1;

        if (name.Contains("email", StringComparison.OrdinalIgnoreCase)) return 0;
        if (name.Contains("identifier", StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.Contains("username", StringComparison.OrdinalIgnoreCase)) return 2;
        if (name.Contains("mobile", StringComparison.OrdinalIgnoreCase) || name.Contains("phone", StringComparison.OrdinalIgnoreCase)) return 3;
        if (name.Equals("name", StringComparison.OrdinalIgnoreCase)) return 4;
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

    private static void AddMessage(List<string> messages, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var trimmed = text.Trim().Trim('"');
        if (trimmed.Length > 1000) trimmed = trimmed[..1000];
        if (messages.Contains(trimmed, StringComparer.Ordinal)) return;
        messages.Add(trimmed);
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
        if (path.Contains("payment", StringComparison.OrdinalIgnoreCase)
            || path.Contains("checkout", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Orders/Verify", StringComparison.OrdinalIgnoreCase))
            return "payment";

        if (path.Contains("login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("auth", StringComparison.OrdinalIgnoreCase))
            return "login";

        return "application";
    }
}
