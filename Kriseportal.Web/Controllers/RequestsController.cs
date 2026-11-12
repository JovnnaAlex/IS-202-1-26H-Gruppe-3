using System.Security.Claims;
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Kriseportal.Web.Services;

namespace Kriseportal.Web.Controllers;

[Authorize(Roles = AppRoles.PublicAuthority + "," + AppRoles.ResourceProvider)]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRequestNotificationSender _notificationSender;

    public RequestsController(
        ApplicationDbContext context,
        IRequestNotificationSender notificationSender)
    {
        _context = context;
        _notificationSender = notificationSender;
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

    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync<IActionResult>(async () =>
    {
        _context.ChangeTracker.Clear();

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var requests = await _context.Requests
            .FromSqlInterpolated(
                $"SELECT * FROM `Requests` WHERE `RequestId` = {id} FOR UPDATE")
            .ToListAsync();

        var request = requests.SingleOrDefault();

        if (request == null)
        {
            return NotFound();
        }

        var match = await _context.Matches
            .AsNoTracking()
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .FirstOrDefaultAsync(m => m.MatchId == request.MatchId);

        if (match?.Need == null
            || match.Resource == null
            || match.Resource.ProviderId != userId)
        {
            return NotFound();
        }

        if (request.Status != RequestStatus.Proposed)
        {
            TempData["RequestError"] =
                "Forespørselen er allerede besvart.";

            return RedirectToAction(nameof(Index));
        }

        request.Status = RequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        var message =
            $"Forespørselen om {request.Quantity} {match.Resource.Unit} av {match.Resource.Name} er avslått.";

        _context.Notifications.Add(new Notification
        {
            UserId = match.Need.PublicAuthorityId,
            RequestId = request.RequestId,
            NeedId = match.NeedId,
            Message = message.Length > 200 ? message[..200] : message
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        // Utviklingsversjonen logger bare.
        // Fremtidig e-post/mobilpush kobles til IRequestNotificationSender.
        await _notificationSender.SendAsync(
            match.Need.PublicAuthorityId,
            message);

        TempData["RequestSuccess"] = "Forespørselen er avslått.";

        return RedirectToAction(nameof(Index));
    });
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Confirm(int id, int? quantity = null)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
    {
        return Challenge();
    }

    // Henter koblingen først. Eierskap og status sjekkes igjen under lås.
    var link = await _context.Requests
        .AsNoTracking()
        .Where(r => r.RequestId == id
            && r.Match != null
            && r.Match.Resource!.ProviderId == userId)
        .Select(r => new
        {
            r.Match!.NeedId,
            r.Match.ResourceId
        })
        .FirstOrDefaultAsync();

    if (link == null)
    {
        return NotFound();
    }

    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync<IActionResult>(async () =>
    {
        // En eventuell ny kjøring skal hente ferske databaseverdier.
        _context.ChangeTracker.Clear();

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

    // Alle bekreftelser låser behov først og ressurs etterpå.
    // Låsene beholdes til transaksjonen avsluttes.
    var needs = await _context.Needs
        .FromSqlInterpolated(
            $"SELECT * FROM `Needs` WHERE `NeedId` = {link.NeedId} FOR UPDATE")
        .ToListAsync();

    var resources = await _context.Resources
        .FromSqlInterpolated(
            $"SELECT * FROM `Resources` WHERE `ResourceId` = {link.ResourceId} FOR UPDATE")
        .ToListAsync();

    var need = needs.SingleOrDefault();
    var resource = resources.SingleOrDefault();

    if (need == null || resource == null)
    {
        return NotFound();
    }

    if (resource.ProviderId != userId)
    {
        return Forbid();
    }

    var requests = await _context.Requests
        .FromSqlInterpolated(
            $"SELECT * FROM `Requests` WHERE `RequestId` = {id} FOR UPDATE")
        .ToListAsync();

    var request = requests.SingleOrDefault();

    if (request == null)
    {
        return NotFound();
    }

    if (request.Status != RequestStatus.Proposed)
    {
        TempData["RequestError"] = "Forespørselen er allerede besvart.";
        return RedirectToAction(nameof(Index));
    }

    if (need.Status == NeedStatus.Resolved
        || resource.Availability != ResourceAvailability.Available
        || resource.CategoryId != need.CategoryId
        || !string.Equals(
            resource.Unit.Trim(),
            need.Unit.Trim(),
            StringComparison.OrdinalIgnoreCase))
    {
        TempData["RequestError"] =
            "Forespørselen kan ikke bekreftes. Behovet må være åpent, og ressursen må være tilgjengelig med samme kategori og måleenhet.";
        return RedirectToAction(nameof(Index));
    }
    
    var coversPeriod =
        (!resource.AvailableFrom.HasValue
         || resource.AvailableFrom.Value <= need.NeededFrom)
        && (!resource.AvailableTo.HasValue
            || (need.NeededTo.HasValue
                && resource.AvailableTo.Value >= need.NeededTo.Value));

    if (!coversPeriod)
    {
        TempData["RequestError"] =
            "Ressursen er ikke tilgjengelig gjennom hele behovsperioden.";

        return RedirectToAction(nameof(Index));
    }
    
    var confirmedForNeed = await _context.Requests
        .Where(r => r.Status == RequestStatus.Confirmed
            && r.Match != null
            && r.Match.NeedId == need.NeedId)
        .SumAsync(r => (int?)r.Quantity) ?? 0;

    var confirmedForResource = await _context.Requests
        .Where(r => r.Status == RequestStatus.Confirmed
            && r.Match != null
            && r.Match.ResourceId == resource.ResourceId)
        .SumAsync(r => (int?)r.Quantity) ?? 0;

    var quantityToConfirm = quantity ?? request.Quantity;

    var maxConfirmable = Math.Max(0, Math.Min(
        request.Quantity,
        Math.Min(
            need.Quantity - confirmedForNeed,
            resource.Quantity - confirmedForResource)));

    if (quantityToConfirm < 1 || quantityToConfirm > maxConfirmable)
    {
        TempData["RequestError"] = maxConfirmable == 0
            ? "Behovet er allerede dekket, eller ressursen har ingen ledig kapasitet."
            : $"Du kan bekrefte opptil {maxConfirmable} {resource.Unit}. Velg antall og prøv igjen.";

        return RedirectToAction(nameof(Index));
    }

    request.Quantity = quantityToConfirm;

    request.Status = RequestStatus.Confirmed;
    request.RespondedAt = DateTime.UtcNow;

    var fullyCovered =
        confirmedForNeed + request.Quantity >= need.Quantity;

    // Behovet går gjennom Vurderes før det eventuelt blir Tildelt.
    if (need.Status == NeedStatus.New)
    {
        need.Status = NeedStatus.UnderReview;
    }

    if (fullyCovered && need.Status == NeedStatus.UnderReview)
    {
        need.Status = NeedStatus.Assigned;
    }

    var responseMessage =
        $"Forespørselen om {request.Quantity} {resource.Unit} av {resource.Name} er bekreftet.";

    _context.Notifications.Add(new Notification
    {
        UserId = need.PublicAuthorityId,
        RequestId = request.RequestId,
        NeedId = need.NeedId,
        Message = responseMessage.Length > 200
            ? responseMessage[..200]
            : responseMessage
    });

    if (fullyCovered)
    {
        var coveredMessage =
            $"Behovet «{need.Title}» er nå fullt dekket: {need.Quantity} av {need.Quantity} {need.Unit} bekreftet.";

        _context.Notifications.Add(new Notification
        {
            UserId = need.PublicAuthorityId,
            RequestId = request.RequestId,
            NeedId = need.NeedId,
            Message = coveredMessage.Length > 200
                ? coveredMessage[..200]
                : coveredMessage
        });
    }
    
    await _context.Notifications
        .Where(n => n.UserId == userId
                    && n.RequestId == request.RequestId
                    && !n.IsRead)
        .ExecuteUpdateAsync(setters =>
            setters.SetProperty(n => n.IsRead, true));
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    
    // Utviklingsversjonen logger bare.
    // Fremtidig e-post/mobilpush kobles til IRequestNotificationSender.
    await _notificationSender.SendAsync(
        need.PublicAuthorityId,
        responseMessage);

    if (fullyCovered)
    {
        await _notificationSender.SendAsync(
            need.PublicAuthorityId,
            "Behovet er nå fullt dekket av bekreftede ressurser.");
    }

    TempData["RequestSuccess"] = fullyCovered
        ? "Forespørselen er bekreftet. Behovet er nå fullt dekket."
        : "Forespørselen er bekreftet.";

    return RedirectToAction(nameof(Index));
    });
}
}
    