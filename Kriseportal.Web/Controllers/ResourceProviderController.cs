using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models;
using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Controllers;

// Sara: Egen controller for ressursleverandørportalen.
// Sara: Kun innloggede ressursleverandører har tilgang.
[Authorize(Roles = AppRoles.ResourceProvider)]
public class ResourceProviderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResourceProviderController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Sara: Viser ressursleverandørens dashboard.
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // Sara: Henter bare ressurser som tilhører innlogget leverandør.
    [HttpGet]
    public async Task<IActionResult> MyResources()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var resources = await _context.Resources
            .AsNoTracking()
            .Include(resource => resource.Category)
            .Where(resource => resource.ProviderId == userId)
            .OrderByDescending(resource => resource.CreatedAt)
            .ToListAsync();

        return View(resources);
    }

    // Sara: Aktiverer eller deaktiverer en ressurs som brukeren eier.
    // Sara: Endringen utføres som POST og beskyttes mot CSRF.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var userId = _userManager.GetUserId(User);

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

            var resources = await _context.Resources
                .FromSqlInterpolated(
                    $"SELECT * FROM `Resources` WHERE `ResourceId` = {id} FOR UPDATE")
                .ToListAsync();

            var resource = resources.SingleOrDefault();

            if (resource == null || resource.ProviderId != userId)
            {
                return NotFound();
            }

            var hasConfirmedRequests = await _context.Requests.AnyAsync(r =>
                r.Status == RequestStatus.Confirmed
                && r.Match != null
                && r.Match.ResourceId == id);

            if (resource.Availability == ResourceAvailability.Reserved
                || hasConfirmedRequests)
            {
                TempData["ResourceMessage"] =
                    "Ressursen er reservert eller har bekreftede forespørsler og kan ikke aktiveres eller deaktiveres her.";

                return RedirectToAction(nameof(MyResources));
            }

            resource.Availability =
                resource.Availability == ResourceAvailability.Available
                    ? ResourceAvailability.Unavailable
                    : ResourceAvailability.Available;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["ResourceMessage"] =
                resource.Availability == ResourceAvailability.Available
                    ? "Ressursen er aktivert."
                    : "Ressursen er deaktivert.";

            return RedirectToAction(nameof(MyResources));
        });
    }

    // Sara: Åpner redigeringssiden med eksisterende ressursinformasjon.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var resource = await _context.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == id && r.ProviderId == userId);

        if (resource == null) return NotFound();

        var model = new ResourceViewModel
        {
            Name = resource.Name,
            CategoryId = resource.CategoryId,
            Description = resource.Description,
            Quantity = resource.Quantity,
            Unit = resource.Unit,
            Availability = resource.Availability,
            AvailableFrom = resource.AvailableFrom,
            AvailableTo = resource.AvailableTo,
            Municipality = resource.Municipality,
            Latitude = (double)resource.Latitude,
            Longitude = (double)resource.Longitude,
            ContactName = resource.ContactName,
            ContactPhone = resource.ContactPhone
        };

        await PrepareEditViewAsync(resource);
        return View(model);
    }

    // Sara: Lagrer endringer bare på ressurser som brukeren selv eier.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResourceViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

                var resources = await _context.Resources
                    .FromSqlInterpolated(
                        $"SELECT * FROM `Resources` WHERE `ResourceId` = {id} FOR UPDATE")
                    .ToListAsync();

                var resource = resources.SingleOrDefault();

                if (resource == null || resource.ProviderId != userId)
                {
                    return NotFound();
                }

                var hasConfirmedRequests = await _context.Requests.AnyAsync(r =>
                    r.Status == RequestStatus.Confirmed
                    && r.Match != null
                    && r.Match.ResourceId == id);

                if (hasConfirmedRequests)
                {
                    TempData["ResourceMessage"] =
                        "Ressursen har bekreftede forespørsler og kan ikke redigeres her.";

                    return RedirectToAction(nameof(MyResources));
                }
                

        // Sara: Reserverte ressurser kan bare få endret kontaktinformasjon
        // gjennom den separate handlingen EditReservedContact.
        if (resource.Availability == ResourceAvailability.Reserved)
        {
            TempData["ResourceMessage"] = "Ressursen er reservert. Du kan bare oppdatere kontaktinformasjonen.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Sara: En bruker skal ikke kunne sette en ressurs til Reservert selv.
        if (model.Availability == ResourceAvailability.Reserved ||
            !Enum.IsDefined(typeof(ResourceAvailability), model.Availability))
        {
            ModelState.AddModelError(nameof(model.Availability), "Velg Tilgjengelig eller Ikke tilgjengelig.");
        }

        if (!model.CategoryId.HasValue ||
            !await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId.Value))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Du må velge en gyldig ressurskategori.");
        }

        if (!ModelState.IsValid)
        {
            await PrepareEditViewAsync(resource);
            return View(model);
        }

        // Sara: Oppdaterer bare redigerbare felter. Eier, ID og registreringsdato beholdes.
        resource.Name = model.Name;
        resource.CategoryId = model.CategoryId!.Value;
        resource.Description = model.Description;
        resource.Quantity = model.Quantity;
        resource.Unit = model.Unit;
        resource.Availability = model.Availability;
        resource.AvailableFrom = model.AvailableFrom;
        resource.AvailableTo = model.AvailableTo;
        resource.Municipality = model.Municipality;
        resource.Latitude = Math.Round(Convert.ToDecimal(model.Latitude!.Value), 6);
        resource.Longitude = Math.Round(Convert.ToDecimal(model.Longitude!.Value), 6);
        resource.ContactName = model.ContactName;
        resource.ContactPhone = model.ContactPhone;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["ResourceMessage"] = "Endringene i ressursen er lagret.";
        return RedirectToAction(nameof(MyResources));
            });
    }
    

    // Sara: Tillater kun endring av kontaktinformasjon for reserverte ressurser.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReservedContact(
        int id,
        [Required, MaxLength(100)] string contactName,
        [Required, MaxLength(20), RegularExpression(@"^\+?[\d\s]{8,20}$")] string contactPhone)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.ResourceId == id && r.ProviderId == userId);

        if (resource == null) return NotFound();
        if (resource.Availability != ResourceAvailability.Reserved)
        {
            TempData["ResourceMessage"] = "Bruk det vanlige redigeringsskjemaet for denne ressursen.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (!ModelState.IsValid)
        {
            TempData["ResourceMessage"] = "Kontroller kontaktperson og telefonnummer før du lagrer.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        resource.ContactName = contactName;
        resource.ContactPhone = contactPhone;
        await _context.SaveChangesAsync();

        TempData["ResourceMessage"] = "Kontaktinformasjonen er oppdatert.";
        return RedirectToAction(nameof(MyResources));
    }

    // Sara: Sletter kun egne ressurser og aldri ressurser med status Reservert.
    [HttpPost]
    [ValidateAntiForgeryToken]
    
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

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

            // Samme ressurslås som ved bekreftelse.
            var resources = await _context.Resources
                .FromSqlInterpolated(
                    $"SELECT * FROM `Resources` WHERE `ResourceId` = {id} FOR UPDATE")
                .ToListAsync();

            var resource = resources.SingleOrDefault();

            if (resource == null || resource.ProviderId != userId)
            {
                return NotFound();
            }

            var hasConfirmedRequests = await _context.Requests.AnyAsync(r =>
                r.Status == RequestStatus.Confirmed
                && r.Match != null
                && r.Match.ResourceId == id);

            if (resource.Availability == ResourceAvailability.Reserved
                || hasConfirmedRequests)
            {
                TempData["ResourceMessage"] =
                    "Ressursen kan ikke slettes fordi den er reservert eller har bekreftede forespørsler.";

                return RedirectToAction(nameof(MyResources));
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["ResourceMessage"] = "Ressursen er slettet.";
            return RedirectToAction(nameof(MyResources));
        });
    }

    // Sara: Sender nødvendige kategorier og låste ressursopplysninger til visningen.
    private async Task PrepareEditViewAsync(Resource resource)
    {
        ViewBag.Categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.ResourceId = resource.ResourceId;
        ViewBag.CreatedAt = resource.CreatedAt;
        ViewBag.IsReserved = resource.Availability == ResourceAvailability.Reserved;
    }
}
