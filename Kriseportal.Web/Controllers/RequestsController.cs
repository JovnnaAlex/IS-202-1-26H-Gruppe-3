using System.Security.Claims;
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Controllers;

[Authorize(Roles = AppRoles.PublicAuthority + "," + AppRoles.ResourceProvider)]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var isProvider = User.IsInRole(AppRoles.ResourceProvider);

        var query = _context.Requests
            .AsNoTracking()
            .Include(r => r.Match)
            .ThenInclude(m => m!.Need)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Resource)
            .Where(r => r.Match != null);

        // Leverandøren ser forespørsler om sine egne ressurser.
        // Offentlig aktør ser forespørsler fra sine egne behov.
        query = isProvider
            ? query.Where(r => r.Match!.Resource!.ProviderId == userId)
            : query.Where(r => r.Match!.Need!.PublicAuthorityId == userId);

        var requests = await query
            .OrderBy(r => r.Status == RequestStatus.Proposed ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewData["IsProvider"] = isProvider;

        return View(requests);
    }
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        // Kontrollerer eierskap og status i samme databaseoperasjon.
        // En allerede besvart forespørsel kan ikke endres.
        var updated = await _context.Requests
            .Where(r => r.RequestId == id
                        && r.Status == RequestStatus.Proposed
                        && r.Match != null
                        && r.Match.Resource != null
                        && r.Match.Resource.ProviderId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, RequestStatus.Rejected)
                .SetProperty(r => r.RespondedAt, (DateTime?)DateTime.UtcNow));

        if (updated == 0)
        {
            TempData["RequestError"] =
                "Forespørselen kunne ikke avslås. Den kan allerede være besvart eller utilgjengelig.";
        }
        else
        {
            TempData["RequestSuccess"] = "Forespørselen er avslått.";
        }

        return RedirectToAction(nameof(Index));
    }
}