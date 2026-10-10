using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LadiesDressStore.Web.Controllers;

[Authorize]
[Route("Notifications")]
public sealed class NotificationsController(ApiService api, IConfiguration configuration) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var notifications = await api.GetNotificationsAsync(0);
            ResolveProductImages(notifications);
            return View(notifications);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Notifications") });
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = "Notifications are temporarily unavailable. Please try again.";
            return View(new NotificationListViewModel());
        }
        catch (JsonException)
        {
            ViewBag.Error = "Notifications are temporarily unavailable. Please try again.";
            return View(new NotificationListViewModel());
        }
    }

    [HttpGet("Recent")]
    public async Task<IActionResult> Recent()
    {
        try
        {
            var notifications = await api.GetNotificationsAsync(8);
            ResolveProductImages(notifications);
            return Json(notifications);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return Unauthorized(new
            {
                message = "Your sign-in session has expired. Sign in again to view notifications.",
                loginUrl = Url.Action("Login", "Account", new { returnUrl = Url.Action("Index", "Notifications") })
            });
        }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Notifications are temporarily unavailable." }); }
        catch (JsonException) { return StatusCode(502, new { message = "Notifications are temporarily unavailable." }); }
    }

    [HttpPost("{id:int}/Read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int id)
    {
        try { return Json(new { success = await api.MarkNotificationReadAsync(id) }); }
        catch (HttpRequestException) { return StatusCode(502, new { success = false }); }
    }

    [HttpPost("{id:int}/Dismiss")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id)
    {
        try { return Json(new { success = await api.DismissNotificationAsync(id) }); }
        catch (HttpRequestException) { return StatusCode(502, new { success = false }); }
    }

    private void ResolveProductImages(NotificationListViewModel notifications)
    {
        foreach (var item in notifications.Items)
            item.ProductImageUrl = ImageUrlHelper.Resolve(item.ProductImageUrl, configuration["ApiBaseUrl"]);
    }
}
