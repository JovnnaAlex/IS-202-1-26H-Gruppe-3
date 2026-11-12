using System.Security.Claims;
using Kriseportal.Web.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Unread()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead);

        var count = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.IsUrgent)
            .ThenByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .Take(20)
            .Select(n => new
            {
                id = n.NotificationId,
                message = n.Message,
                isUrgent = n.IsUrgent,
                needId = n.NeedId,
                requestId = n.RequestId,
                canRespond = _context.Requests.Any(r =>
                    r.RequestId == n.RequestId
                    && r.Status == RequestStatus.Proposed
                    && r.Match != null
                    && r.Match.Resource!.ProviderId == userId)
            })
            .ToListAsync();

        var pendingCount = User.IsInRole(AppRoles.ResourceProvider)
            ? await _context.Requests.CountAsync(r =>
                r.Status == RequestStatus.Proposed
                && r.Match != null
                && r.Match.Resource!.ProviderId == userId)
            : 0;
        var responseCount = User.IsInRole(AppRoles.PublicAuthority)
            ? await _context.Notifications.CountAsync(n =>
                n.UserId == userId
                && !n.IsRead
                && n.RequestId != null)
            : 0;
        
        return Json(new { count, notifications, pendingCount, responseCount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _context.Notifications
            .Where(n => n.NotificationId == id && n.UserId == userId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(n => n.IsRead, true));

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .ToListAsync();

        return View(notifications);
    }
}