using Kriseportal.Web.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Kriseportal.Web.Services;


namespace Kriseportal.Web.Controllers
{
    [Authorize(Roles = AppRoles.PublicAuthority)]
    public class MatchingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRequestNotificationSender _notificationSender;

        public MatchingController(
            ApplicationDbContext context,
            IRequestNotificationSender notificationSender)
        {
            _context = context;
            _notificationSender = notificationSender;
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

            var remaining = Math.Max(0, behov.Quantity - confirmedQuantity);

            var resources = await _context.Resources
                .AsNoTracking()
                .Where(r => r.Availability == ResourceAvailability.Available
                            && r.CategoryId == behov.CategoryId
                            && (r.AvailableFrom == null
                                || r.AvailableFrom <= behov.NeededFrom)
                            && (r.AvailableTo == null
                                || (behov.NeededTo != null
                                    && r.AvailableTo >= behov.NeededTo)))
                .ToListAsync();

            // Sammenligner enheter uten forskjell på store/små bokstaver.
            resources = resources
                .Where(r => string.Equals(
                    r.Unit.Trim(),
                    behov.Unit.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var resourceIds = resources.Select(r => r.ResourceId).ToList();

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

            var distances = resources.ToDictionary(
                r => r.ResourceId,
                r => DistanceKm(
                    (double)behov.Latitude,
                    (double)behov.Longitude,
                    (double)r.Latitude,
                    (double)r.Longitude));

            var contactedResourceIds = requests
                .Where(r => r.Status != RequestStatus.Rejected)
                .Select(r => r.Match!.ResourceId)
                .ToHashSet();

            var suggestions = resources
                .Where(r => remaining > 0
                    && behov.Status != NeedStatus.Resolved
                    && availableQuantities[r.ResourceId] > 0
                    && !contactedResourceIds.Contains(r.ResourceId))
                .OrderByDescending(r =>
                    Math.Min(remaining, availableQuantities[r.ResourceId]))
                .ThenBy(r => distances[r.ResourceId])
                .ThenBy(r => r.ResourceId)
                .Take(3)
                .ToList();

            var model = new MatchingSuggestionsViewModel
            {
                NeedId = needID,
                Need = behov,
                Requests = requests,
                ConfirmedQuantity = confirmedQuantity,
                AvailableQuantities = availableQuantities,
                DistancesKm = distances,
                Resources = suggestions
            };

            return View("Suggestions", model);
        }

        // Luftlinjeavstand, ikke kjøreavstand.
        private static double DistanceKm(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            const double earthRadiusKm = 6371;

            var lat1 = latitude1 * Math.PI / 180;
            var lat2 = latitude2 * Math.PI / 180;
            var deltaLat = (latitude2 - latitude1) * Math.PI / 180;
            var deltaLon = (longitude2 - longitude1) * Math.PI / 180;

            var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
                + Math.Cos(lat1) * Math.Cos(lat2)
                * Math.Pow(Math.Sin(deltaLon / 2), 2);

            return earthRadiusKm * 2
                * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(
            SendRequestViewModel model,
            bool fromSuggestions = false)
        {
            if (!ModelState.IsValid)
            {
                TempData["RequestError"] =
                    "Behov, ressurs og antall må være gyldige.";

                return RedirectToAction("Index", "Requests");
            }

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
                    .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

                var needs = await _context.Needs
                    .FromSqlInterpolated(
                        $"SELECT * FROM `Needs` WHERE `NeedId` = {model.NeedId} FOR UPDATE")
                    .ToListAsync();

                var behov = needs.SingleOrDefault();

                if (behov == null)
                {
                    return NotFound();
                }

                if (behov.PublicAuthorityId != userId)
                {
                    return Forbid();
                }

                var resources = await _context.Resources
                    .FromSqlInterpolated(
                        $"SELECT * FROM `Resources` WHERE `ResourceId` = {model.ResourceId} FOR UPDATE")
                    .ToListAsync();

                var ressurs = resources.SingleOrDefault();

                if (ressurs == null)
                {
                    return NotFound();
                }

                if (behov.Status == NeedStatus.Resolved)
                {
                    TempData["RequestError"] =
                        "Behovet er løst og kan ikke få nye forespørsler.";

                    return RedirectToAction("Index", "Requests");
                }

                if (ressurs.Availability != ResourceAvailability.Available
                    || ressurs.CategoryId != behov.CategoryId
                    || !string.Equals(
                        ressurs.Unit.Trim(),
                        behov.Unit.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["RequestError"] =
                        "Ressursen må være tilgjengelig og ha samme kategori og måleenhet som behovet.";

                    return RedirectToAction("Index", "Requests");
                }

                var coversPeriod =
                    (!ressurs.AvailableFrom.HasValue
                     || ressurs.AvailableFrom.Value <= behov.NeededFrom)
                    && (!ressurs.AvailableTo.HasValue
                        || (behov.NeededTo.HasValue
                            && ressurs.AvailableTo.Value >= behov.NeededTo.Value));

                if (!coversPeriod)
                {
                    TempData["RequestError"] =
                        "Ressursen er ikke tilgjengelig gjennom hele behovsperioden.";

                    return RedirectToAction("Index", "Requests");
                }
                
                var confirmedForNeed = await _context.Requests
                    .Where(r => r.Status == RequestStatus.Confirmed
                        && r.Match != null
                        && r.Match.NeedId == model.NeedId)
                    .SumAsync(r => (int?)r.Quantity) ?? 0;

                var confirmedForResource = await _context.Requests
                    .Where(r => r.Status == RequestStatus.Confirmed
                        && r.Match != null
                        && r.Match.ResourceId == model.ResourceId)
                    .SumAsync(r => (int?)r.Quantity) ?? 0;

                var remainingNeed =
                    Math.Max(0, behov.Quantity - confirmedForNeed);

                var availableResource =
                    Math.Max(0, ressurs.Quantity - confirmedForResource);

                if (model.Quantity > remainingNeed)
                {
                    TempData["RequestError"] =
                        $"Behovet mangler bare {remainingNeed} {behov.Unit}.";

                    return RedirectToAction("Index", "Requests");
                }

                if (model.Quantity > availableResource)
                {
                    TempData["RequestError"] =
                        $"Ressursen har bare {availableResource} {ressurs.Unit} ledig.";

                    return RedirectToAction("Index", "Requests");
                }

                var requestAlreadyExists = await _context.Requests.AnyAsync(r =>
                    r.Match != null
                    && r.Match.NeedId == model.NeedId
                    && r.Match.ResourceId == model.ResourceId
                    && r.Status != RequestStatus.Rejected);

                if (requestAlreadyExists)
                {
                    TempData["RequestError"] =
                        "Du har allerede en ventende eller bekreftet forespørsel til denne ressursen for behovet.";

                    return RedirectToAction("Index", "Requests");
                }

                var match = await _context.Matches.FirstOrDefaultAsync(m =>
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

                var request = new Request
                {
                    Match = match,
                    Quantity = model.Quantity,
                    Status = RequestStatus.Proposed
                };

                _context.Requests.Add(request);

                if (behov.Status == NeedStatus.New)
                {
                    behov.Status = NeedStatus.UnderReview;
                }

                // Lagrer forespørselen først, slik at den får en RequestId.
                await _context.SaveChangesAsync();

                var notificationMessage =
                    $"Ny forespørsel: {model.Quantity} {ressurs.Unit} av {ressurs.Name}.";

                _context.Notifications.Add(new Notification
                {
                    UserId = ressurs.ProviderId,
                    RequestId = request.RequestId,
                    NeedId = behov.NeedId,
                    Message = notificationMessage.Length > 200
                        ? notificationMessage[..200]
                        : notificationMessage,
                    IsUrgent = behov.Priority == NeedPriority.Acute
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Utviklingsversjonen logger bare.
                // Fremtidig e-post/mobilpush kobles til IRequestNotificationSender.
                await _notificationSender.SendAsync(
                    ressurs.ProviderId,
                    notificationMessage);
                
                TempData["RequestSuccess"] =
                    "Forespørselen er sendt til ressursleverandøren og venter på svar.";

                if (fromSuggestions)
                {
                    return RedirectToAction(
                        nameof(Suggestions),
                        new { needID = model.NeedId });
                }

                return RedirectToAction("Index", "Requests");
            });
        }
    }
}
