using LadiesDressStore.Web.Middleware;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class ClientLogController(IErrorLog errorLog) : Controller
{
    [HttpPost("/client-log")]
    [IgnoreAntiforgeryToken]
    public IActionResult Create(string? message, string? detail, string? path)
    {
        var text = (message ?? "").Trim();
        if (text.Length == 0) return NoContent();
        if (text.Length > 1000) text = text[..1000];

        var page = (path ?? "").Trim();
        if (page.Length > 300) page = page[..300];
        var stack = (detail ?? "").Trim();
        if (stack.Length > 4000) stack = stack[..4000];

        var area = page.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
                   page.Contains("checkout", StringComparison.OrdinalIgnoreCase)
            ? "payment"
            : "browser";

        errorLog.Write(ErrorLogMiddleware.Username(User), text, stack, page, area);
        return NoContent();
    }
}
