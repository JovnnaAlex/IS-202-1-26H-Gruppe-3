using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

// Sara: Lagt til for å hente ressurser fra databasen.
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Controllers;

public class ResourceController : Controller
{
    // Sara: Kobler ResourceController til databasen.
    private readonly ApplicationDbContext _context;

    // Henter informasjon om den innloggede brukeren.
    private readonly UserManager<ApplicationUser> _userManager;

    public ResourceController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Sara: MIDLERTIDIG TEST.
    // Lager en eksempelressurs kun i minnet.
    // Den lagres ikke i databasen og skal fjernes etter testing.
    private static Resource CreateTestResource()
    {
        return new Resource
        {
            ResourceId = -1,
            Name = "Strømaggregat",
            Description = "Strømaggregat som kan brukes ved strømbrudd og andre beredskapssituasjoner.",

            CategoryId = 1,
            Category = new Category
            {
                CategoryId = 1,
                Name = "Utstyr"
            },

            Quantity = 2,
            Unit = "stk",
            Availability = ResourceAvailability.Available,

            Municipality = "Kristiansand",

            // Sara: Eksempelkoordinater for å teste kartet.
            Latitude = 58.1467m,
            Longitude = 7.9956m,

            ContactName = "Testperson",
            ContactPhone = "00000000",

            // Sara: Testressursen har ingen ekte leverandør.
            ProviderId = "midlertidig-test"
        };
    }

    // Sara: Viser oversikten over tilgjengelige ressurser.
    // B12: Bare offentlige aktører kan se ledige ressurser.
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var resources = await _context.Resources
            .Include(r => r.Category)
            .Where(r => r.Availability == ResourceAvailability.Available)
            .OrderBy(r => r.Name)
            .ToListAsync();

        // Sara: MIDLERTIDIG TEST.
        // Legger testressursen til i listen uten å endre databasen.
        resources.Add(CreateTestResource());

        resources = resources
            .OrderBy(r => r.Name)
            .ToList();

        return View(resources);
    }

    // Sara: Henter én bestemt ressurs fra databasen
    // og viser den på den nye detaljsiden.
    // B12: Bare offentlige aktører kan åpne detaljer om ressurser.
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpGet]
    
    public async Task<IActionResult> ResourceDetails(int id)
    {
        // Sara: MIDLERTIDIG TEST.
        // Åpner testressursen når brukeren klikker "Se detaljer".
        if (id == -1)
        {
            return View(CreateTestResource());
        }

        var resource = await _context.Resources
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r =>
                r.ResourceId == id &&
                r.Availability == ResourceAvailability.Available);

        // Sara: Viser 404 dersom ressursen ikke finnes
        // eller ikke er tilgjengelig.
        if (resource == null)
        {
            return NotFound();
        }

        // Åpner Views/Resource/ResourceDetails.cshtml.
        return View(resource);
    }

    // Viser valgt ressurs og den offentlige aktørens aktuelle behov.
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpGet]
    public async Task<IActionResult> SendRequest(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var resource = await _context.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.ResourceId == id &&
                r.Availability == ResourceAvailability.Available);

        if (resource == null)
        {
            return NotFound();
        }

        var confirmedQuantity = await _context.Requests
            .Where(r => r.Status == RequestStatus.Confirmed
                        && r.Match != null
                        && r.Match.ResourceId == id)
            .SumAsync(r => (int?)r.Quantity) ?? 0;

        var needs = await _context.Needs
            .AsNoTracking()
            .Where(n => n.PublicAuthorityId == userId
                        && n.CategoryId == resource.CategoryId
                        && n.Status != NeedStatus.Resolved)
            .Where(n => (_context.Requests
                .Where(r => r.Status == RequestStatus.Confirmed
                            && r.Match != null
                            && r.Match.NeedId == n.NeedId)
                .Sum(r => (int?)r.Quantity) ?? 0) < n.Quantity)
            .Where(n => !_context.Requests.Any(r =>
                r.Match != null
                && r.Match.NeedId == n.NeedId
                && r.Match.ResourceId == id
                && r.Status != RequestStatus.Rejected))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        needs = needs
            .Where(n => string.Equals(
                n.Unit.Trim(),
                resource.Unit.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .Where(n =>
                (!resource.AvailableFrom.HasValue
                 || resource.AvailableFrom.Value <= n.NeededFrom)
                && (!resource.AvailableTo.HasValue
                    || (n.NeededTo.HasValue
                        && resource.AvailableTo.Value >= n.NeededTo.Value)))
            .ToList();
        
        var model = new ResourceRequestViewModel
        {
            Resource = resource,
            AvailableQuantity = Math.Max(
                0, resource.Quantity - confirmedQuantity),
            Needs = needs
        };

        return View(model);
    }
    // Henter kategorier fra databasen når registreringsskjemaet åpnes.
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View();
    }

    // Validerer ressursregistreringen og viser forhåndsvisningen.
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResourceViewModel model)
    {
        // Kontrollerer at den valgte kategorien finnes i databasen.
    
        if (model.CategoryId.HasValue &&
            !await _context.Categories.AnyAsync(c =>
                c.CategoryId == model.CategoryId.Value))
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Du må velge en gyldig ressurskategori.");
        }

        if (!ModelState.IsValid)
        {
            // Henter kategoriene på nytt dersom skjemaet har feil.
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(model);
        }

        // Henter kategorinavnet til bekreftelsessiden.
        ViewBag.CategoryName = await _context.Categories
            .Where(c => c.CategoryId == model.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        // Foreløpig ingen lagring i databasen.
        return View("Details", model);
    }

    // Sender brukeren tilbake til skjemaet med tidligere utfylte opplysninger.
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPreview(ResourceViewModel model)
    {
        // Henter kategoriene på nytt slik at nedtrekkslisten fungerer.
        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Viser skjemaet med informasjonen brukeren allerede har fylt ut.
        return View("Create", model);
    }

    // Viser bekreftelsessiden når brukeren godkjenner opplysningene.
    [Authorize(Roles = AppRoles.ResourceProvider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPreview(ResourceViewModel model)
    {
        // Kontrollerer at den valgte kategorien finnes i databasen.
        if (model.CategoryId.HasValue &&
            !await _context.Categories.AnyAsync(c =>
                c.CategoryId == model.CategoryId.Value))
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Du må velge en gyldig ressurskategori.");
        }

        // Kontrollerer at alle opplysningene er gyldige.
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View("Create", model);
        }
        // Henter brukeren som registrerer ressursen.
        var user = await _userManager.GetUserAsync(User);

        // Kontrollerer at brukeren finnes.
        if (user == null)
        {
            return Challenge();
        }
        // Oppretter en ny ressurs med informasjon fra skjemaet.
        var resource = new Resource
        {
            Name = model.Name,
            Description = model.Description,
            CategoryId = model.CategoryId!.Value,
            Quantity = model.Quantity,
            Unit = model.Unit,
            Availability = model.Availability,

            AvailableFrom = model.AvailableFrom,
            AvailableTo = model.AvailableTo,

            Municipality = model.Municipality,
            Latitude = Math.Round(Convert.ToDecimal(model.Latitude!.Value), 6),
            Longitude = Math.Round(Convert.ToDecimal(model.Longitude!.Value), 6),

            ContactName = model.ContactName,
            ContactPhone = model.ContactPhone,

            ProviderId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        // Legger ressursen til i databasen.
        _context.Resources.Add(resource);

        // Lagrer endringene i MariaDB.
        await _context.SaveChangesAsync();

        // Viser bekreftelsessiden etter vellykket lagring.
        return View("Confirmation");
    }
}

// la til en if statement for modelstate validation Edgar