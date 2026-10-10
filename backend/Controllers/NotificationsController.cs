using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] int take = 8)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        take = take <= 0 ? 0 : Math.Clamp(take, 1, 100);
        var customerStates = db.CustomerNotificationStates.AsNoTracking().Where(x => x.CustomerId == customerId);
        var active = from notification in db.Notifications.AsNoTracking()
                     join state in customerStates on notification.Id equals state.NotificationId into stateRows
                     from state in stateRows.DefaultIfEmpty()
                     where state == null || state.DismissedDate == null
                     select new { notification, readDate = (DateTime?)state.ReadDate };
        var unreadCount = await active.CountAsync(x => x.readDate == null);
        var ordered = active.OrderByDescending(x => x.notification.CreatedDate);
        var items = take > 0
            ? await ordered.Take(take).Select(x => new { id = x.notification.Id, title = x.notification.Title, message = x.notification.Message, link = x.notification.Link, productImageUrl = x.notification.Product == null ? null : x.notification.Product.ImageUrl, createdDate = x.notification.CreatedDate, readDate = x.readDate }).ToListAsync()
            : await ordered.Select(x => new { id = x.notification.Id, title = x.notification.Title, message = x.notification.Message, link = x.notification.Link, productImageUrl = x.notification.Product == null ? null : x.notification.Product.ImageUrl, createdDate = x.notification.CreatedDate, readDate = x.readDate }).ToListAsync();
        return Ok(new { unreadCount, items });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        if (!await db.Notifications.AnyAsync(x => x.Id == id)) return NotFound();
        var state = await db.CustomerNotificationStates.FindAsync(customerId, id);
        if (state is null)
        {
            state = new CustomerNotificationState { CustomerId = customerId, NotificationId = id, ReadDate = DateTime.UtcNow };
            db.CustomerNotificationStates.Add(state);
        }
        else state.ReadDate ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Dismiss(int id)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        if (!await db.Notifications.AnyAsync(x => x.Id == id)) return NotFound();
        var state = await db.CustomerNotificationStates.FindAsync(customerId, id);
        if (state is null)
        {
            state = new CustomerNotificationState { CustomerId = customerId, NotificationId = id, DismissedDate = DateTime.UtcNow };
            db.CustomerNotificationStates.Add(state);
        }
        else state.DismissedDate ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private bool TryGetCustomerId(out int customerId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);
}




