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

    // Sara: Viser ressursleverandørens dashboard med faktiske tall fra databasen.
[HttpGet]
public async Task<IActionResult> Index()
{
    var userId = _userManager.GetUserId(User);

    if (string.IsNullOrEmpty(userId))
    {
        return Challenge();
    }

    // Sara: Teller ressursene som tilhører innlogget leverandør.
    ViewBag.TotalResources = await _context.Resources
        .CountAsync(r => r.ProviderId == userId);

    // Sara: Teller tilgjengelige ressurser.
    ViewBag.AvailableResources = await _context.Resources
        .CountAsync(r =>
            r.ProviderId == userId &&
            r.Availability == ResourceAvailability.Available);

    // Sara: Teller nye forespørsler som venter på svar.
    ViewBag.NewRequests = await _context.Requests
        .CountAsync(r =>
            r.Match != null &&
            r.Match.Resource != null &&
            r.Match.Resource.ProviderId == userId &&
            r.Status == RequestStatus.Proposed);

    // Sara: Henter de fem nyeste ressursene til dashboardet.
    ViewBag.RecentResources = await _context.Resources
        .AsNoTracking()
        .Include(r => r.Category)
        .Where(r => r.ProviderId == userId)
        .OrderByDescending(r => r.CreatedAt)
        .ThenByDescending(r => r.ResourceId)
        .Take(5)
        .ToListAsync();

    // Sara: Henter de fem nyeste forespørslene for egne ressurser.
    ViewBag.RecentRequests = await _context.Requests
        .AsNoTracking()
        .Include(r => r.Match)
            .ThenInclude(m => m!.Resource)
        .Where(r => r.Match != null &&
                    r.Match.Resource != null &&
                    r.Match.Resource.ProviderId == userId)
        .OrderByDescending(r => r.CreatedAt)
        .ThenByDescending(r => r.RequestId)
        .Take(5)
        .ToListAsync();

    return View();
}

    // Sara: Viser profilsiden med informasjon om innlogget ressursleverandør.
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        return View(user);
    }

    // Sara: Lagrer profilopplysninger for den innloggede ressursleverandøren.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [Required, MaxLength(100)] string name,
        [Required, EmailAddress, MaxLength(256)] string email,
        [MaxLength(30)] string? phoneNumber,
        [MaxLength(200)] string? address,
        [MaxLength(2000)] string? description)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Kontroller at feltene er riktig utfylt og prøv igjen.";
            return RedirectToAction(nameof(Profile));
        }

        name = name.Trim();
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            TempData["ProfileError"] = "Navn og e-postadresse må fylles ut.";
            return RedirectToAction(nameof(Profile));
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null && existingUser.Id != user.Id)
        {
            TempData["ProfileError"] = "E-postadressen er allerede registrert på en annen konto.";
            return RedirectToAction(nameof(Profile));
        }

        var previousEmail = user.Email;
        user.Name = name;
        user.Email = email;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        user.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        // Sara: Dersom brukernavnet er den gamle e-posten, holdes det synkronisert.
        if (string.Equals(user.UserName, previousEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = email;
        }

        if (!string.Equals(previousEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            // Sara: En ny e-postadresse skal ikke arve gammel e-postbekreftelse.
            user.EmailConfirmed = false;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ProfileError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Profile));
        }

        TempData["ProfileSuccess"] = "Profilopplysningene er lagret.";
        return RedirectToAction(nameof(Profile));
    }

    // Sara: Viser forespørsler knyttet til ressursene
    // som tilhører den innloggede ressursleverandøren.
    [HttpGet]
    public async Task<IActionResult> Requests()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        // Sara: Henter forespørsler med tilhørende ressurs,
        // behov og offentlig aktør.
        var requests = await _context.Requests
            .AsNoTracking()
            .Include(r => r.Match)
                .ThenInclude(m => m!.Resource)
            .Include(r => r.Match)
                .ThenInclude(m => m!.Need)
                    .ThenInclude(n => n!.PublicAuthority)
            .Where(r => r.Match != null &&
                        r.Match.Resource != null &&
                        r.Match.Resource.ProviderId == userId)
            .OrderBy(r => r.Status == RequestStatus.Proposed ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    // Sara: Viser detaljene for en forespørsel som gjelder
    // en ressurs eid av den innloggede leverandøren.
    [HttpGet]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        // Sara: Henter forespørselen med ressurs, behov og avsender.
        // Sara: Eierkontrollen gjøres direkte i databasespørringen.
        var request = await _context.Requests
            .AsNoTracking()
            .Include(r => r.Match)
                .ThenInclude(m => m!.Resource)
            .Include(r => r.Match)
                .ThenInclude(m => m!.Need)
                    .ThenInclude(n => n!.PublicAuthority)
            .FirstOrDefaultAsync(r =>
                r.RequestId == id &&
                r.Match != null &&
                r.Match.Resource != null &&
                r.Match.Resource.ProviderId == userId);

        if (request == null)
        {
            return NotFound();
        }

        return View(request);
    }

    // Sara: Behandler godkjenning eller avslag og lagrer valgfri melding til mottakeren.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondToRequest(int id, string decision, [MaxLength(2000)] string? responseMessage)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (decision != "confirm" && decision != "reject") return BadRequest();

        if (responseMessage?.Length > 2000)
        {
            TempData["RequestMessage"] = "Meldingen kan ikke være lengre enn 2000 tegn.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }

        // Sara: En transaksjon hindrer at to samtidige godkjenninger reserverer samme antall.
        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        var request = await _context.Requests
            .Include(r => r.Match)
                .ThenInclude(m => m!.Resource)
            .FirstOrDefaultAsync(r =>
                r.RequestId == id &&
                r.Match != null &&
                r.Match.Resource != null &&
                r.Match.Resource.ProviderId == userId);

        if (request == null) return NotFound();

        if (request.Status != RequestStatus.Proposed)
        {
            TempData["RequestMessage"] = "Denne forespørselen er allerede behandlet.";
            return RedirectToAction(nameof(RequestDetails), new { id });
        }

        var resource = request.Match!.Resource!;

        if (decision == "confirm")
        {
            if (resource.Availability != ResourceAvailability.Available)
            {
                TempData["RequestMessage"] = "Ressursen er ikke tilgjengelig for godkjenning.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            // Sara: Summerer tidligere godkjente enheter for denne ressursen.
            var alreadyConfirmed = await _context.Requests
                .Where(r => r.Status == RequestStatus.Confirmed &&
                            r.Match != null && r.Match.ResourceId == resource.ResourceId)
                .SumAsync(r => (int?)r.Quantity) ?? 0;

            if (request.Quantity > resource.Quantity - alreadyConfirmed)
            {
                TempData["RequestMessage"] = "Det er ikke nok ledige enheter til å godkjenne forespørselen.";
                return RedirectToAction(nameof(RequestDetails), new { id });
            }

            request.Status = RequestStatus.Confirmed;

            // Sara: Først når alle enhetene er fordelt, markeres hele ressursen som reservert.
            if (alreadyConfirmed + request.Quantity == resource.Quantity)
            {
                resource.Availability = ResourceAvailability.Reserved;
            }
        }
        else
        {
            request.Status = RequestStatus.Rejected;
        }

        request.ResponseMessage = string.IsNullOrWhiteSpace(responseMessage)
            ? null
            : responseMessage.Trim();
        request.RespondedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["RequestMessage"] = decision == "confirm"
            ? "Forespørselen er godkjent."
            : "Forespørselen er avslått.";

        return RedirectToAction(nameof(RequestDetails), new { id });
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
