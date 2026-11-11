using Kriseportal.Web.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace Kriseportal.Web.Controllers
{
    [Authorize(Roles = AppRoles.PublicAuthority)]
    public class MatchingController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MatchingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
public async Task<IActionResult> Suggestions(int needID)
{
    var behov = await _context.Needs
        .AsNoTracking()
        .Include(n => n.Category)
        .FirstOrDefaultAsync(n => n.NeedId == needID);

    if (behov == null)
    {
        return NotFound();
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (behov.PublicAuthorityId != userId)
    {
        return Forbid();
    }

    // Henter forespørsler og svar som gjelder dette behovet.
    var requests = await _context.Requests
        .AsNoTracking()
        .Include(r => r.Match)
            .ThenInclude(m => m!.Resource)
        .Where(r => r.Match != null && r.Match.NeedId == needID)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    var confirmedQuantity = requests
        .Where(r => r.Status == RequestStatus.Confirmed)
        .Sum(r => r.Quantity);

    var resources = await _context.Resources
        .AsNoTracking()
        .Where(r => r.Availability == ResourceAvailability.Available
            && r.CategoryId == behov.CategoryId)
        .OrderBy(r => r.Name)
        .ToListAsync();

    var resourceIds = resources.Select(r => r.ResourceId).ToList();

    // Teller også bekreftelser til andre behov,
    // slik at samme kapasitet ikke foreslås flere ganger.
    var allocatedQuantities = await _context.Requests
        .AsNoTracking()
        .Where(r => r.Status == RequestStatus.Confirmed
            && r.Match != null
            && resourceIds.Contains(r.Match.ResourceId))
        .GroupBy(r => r.Match!.ResourceId)
        .Select(group => new
        {
            ResourceId = group.Key,
            Quantity = group.Sum(r => r.Quantity)
        })
        .ToDictionaryAsync(r => r.ResourceId, r => r.Quantity);

    var availableQuantities = resources.ToDictionary(
        r => r.ResourceId,
        r => Math.Max(
            0,
            r.Quantity - allocatedQuantities.GetValueOrDefault(r.ResourceId)));

    var model = new MatchingSuggestionsViewModel
    {
        NeedId = needID,
        Need = behov,
        Requests = requests,
        ConfirmedQuantity = confirmedQuantity,
        AvailableQuantities = availableQuantities,
        Resources = resources
            .Where(r => availableQuantities[r.ResourceId] > 0)
            .ToList()
    };

    return View("Suggestions", model);
}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendRequest(SendRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Behov, ressurs og antall må være gyldige.");
            }

            var behov = _context.Needs.Find(model.NeedId);
            var ressurs = _context.Resources.Find(model.ResourceId);

            if (behov == null || ressurs == null)
            {
                return NotFound();
            }

            var UserId= User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (behov.PublicAuthorityId != UserId)
            {
                return Forbid();
            }

            if (ressurs.Availability != ResourceAvailability.Available
            || ressurs.CategoryId != behov.CategoryId)
            {
                return BadRequest("Ressursen er ikke tilgjengelig eller har feil kategori.");
            }

            if (behov.Status == NeedStatus.Resolved)
            {
                return BadRequest("Behovet er løst og kan ikke få nye forespørsler.");
            }

// Bare bekreftede forespørsler teller mot behovet.
            var confirmedForNeed = _context.Requests
                .Where(r => r.Status == RequestStatus.Confirmed
                            && r.Match != null
                            && r.Match.NeedId == model.NeedId)
                .Sum(r => (int?)r.Quantity) ?? 0;

// Ressursen kan allerede ha lovet bort enheter til andre behov.
            var confirmedForResource = _context.Requests
                .Where(r => r.Status == RequestStatus.Confirmed
                            && r.Match != null
                            && r.Match.ResourceId == model.ResourceId)
                .Sum(r => (int?)r.Quantity) ?? 0;

            var remainingNeed = Math.Max(0, behov.Quantity - confirmedForNeed);
            var availableResource = Math.Max(0, ressurs.Quantity - confirmedForResource);

            if (model.Quantity > remainingNeed)
            {
                return BadRequest(
                    $"Behovet mangler bare {remainingNeed} {behov.Unit}.");
            }

            if (model.Quantity > availableResource)
            {
                return BadRequest(
                    $"Ressursen har bare {availableResource} {ressurs.Unit} ledig.");
            }

            var requestAlreadyExists = _context.Requests.Any(r =>
            r.Match != null
            && r.Match.NeedId == model.NeedId
            && r.Match.ResourceId == model.ResourceId
            && r.Status != RequestStatus.Rejected);

            if (requestAlreadyExists)
            {
                return BadRequest(
                    "Det finnes allerede en ventende eller bekreftet forespørsel for denne ressursen til behovet.");
            }

            var match = _context.Matches.FirstOrDefault(m =>
            m.NeedId == model.NeedId
            && m.ResourceId == model.ResourceId);

            if (match == null)
            {
                match = new Match
                {
                    NeedId = model.NeedId,
                    ResourceId = model.ResourceId
                };
            }

            var request = new Kriseportal.Web.Models.Entities.Request
            {
                Match = match,
                Quantity = model.Quantity,
                Status = RequestStatus.Proposed
            };

            _context.Requests.Add(request);
            _context.SaveChanges();

            return RedirectToAction("Index", "Requests");

        }
    }
}
