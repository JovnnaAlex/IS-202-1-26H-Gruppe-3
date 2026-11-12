using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;
using Kriseportal.Web.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Kriseportal.Web.Controllers;

// B12: Bare offentlige aktører kan se, registrere og åpne behov
[Authorize(Roles = AppRoles.PublicAuthority)]
public class NeedController : Controller
{

    // Viser oversikten over behov 
    // Dette henter registrerte behov og kategoriene deres fra MariaDB.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var needs = await _context.Needs
            .AsNoTracking()
            .Include(n => n.Category)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(needs);
    }

    // Database og miljø (fra master)
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    // Henter informasjon om den innloggede brukeren, slik at behovet får riktig eier
    private readonly UserManager<ApplicationUser> _userManager;

    public NeedController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _environment = environment;
        _userManager = userManager;
    }


    // Bare innloggede offentlige aktører kan registrere behov
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = new SelectList(
            categories,
            "CategoryId",
            "Name"
        );

        return View();
    }

    // Sjekkes også ved innsending, så ingen kan hoppe over skjemaet og sende direkte
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NeedViewModel model)
    {
        // Kontrollerer at kommunen finnes i den offisielle kommunelisten.
        var municipalityFile = Path.Combine(
            _environment.WebRootPath,
            "data",
            "kommuner.json"
        );

        var municipalityJson = await System.IO.File.ReadAllTextAsync(municipalityFile);

        var municipalities = JsonSerializer.Deserialize<List<string>>(municipalityJson)
                             ?? new List<string>();

        if (!string.IsNullOrWhiteSpace(model.Municipality) &&
            !municipalities.Contains(
                model.Municipality.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Municipality),
                "Du må velge en gyldig kommune fra listen."
            );
        }
        
        // Kontrollerer at prioriteten er en av de fire gyldige verdiene.
        if (!Enum.TryParse<NeedPriority>(model.Priority, out _))
        {
            ModelState.AddModelError(
                nameof(model.Priority),
                "Du må velge en gyldig prioritet."
            );
        }

        // Kontrollerer at sluttdato ikke er før startdato.
        if (model.NeededFrom.HasValue &&
            model.NeededTo.HasValue &&
            model.NeededTo.Value < model.NeededFrom.Value)
        {
            ModelState.AddModelError(
                nameof(model.NeededTo),
                "Sluttdato kan ikke være tidligere enn startdato."
            );
        }
        
        if (!ModelState.IsValid)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories,
                "CategoryId",
                "Name"
            );

            return View(model);
        }

        // Henter kategorien brukeren har valgt.
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == model.CategoryId);

        // Kontrollerer at kategorien faktisk finnes.
        if (category == null)
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Du må velge en gyldig kategori."
            );

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories,
                "CategoryId",
                "Name"
            );

            return View(model);
        }

        // Henter brukeren som registrerer behovet.
        var user = await _userManager.GetUserAsync(User);

        // Sender brukeren til innlogging hvis den ikke finnes.
        if (user == null)
        {
            return Challenge();
        }

        // Oppretter et nytt behov med informasjonen fra skjemaet.
        var need = new Need
        {
            Title = model.Title,
            Description = model.Description,
            CategoryId = category.CategoryId,
            Priority = Enum.Parse<NeedPriority>(model.Priority),
            Quantity = model.Quantity,
            Unit = model.Unit,
            NeededFrom = model.NeededFrom!.Value,
            NeededTo = model.NeededTo,
            Municipality = model.Municipality.Trim(),

            // Databasen lagrer koordinater som decimal med 6 desimaler
            Latitude = Math.Round(Convert.ToDecimal(model.Latitude!.Value), 6),
            Longitude = Math.Round(Convert.ToDecimal(model.Longitude!.Value), 6),

            ContactName = model.ContactName,
            ContactPhone = model.ContactPhone,

            Status = NeedStatus.New,
            PublicAuthorityId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Legger behovet til og lagrer det i MariaDB.
        _context.Needs.Add(need);
        await _context.SaveChangesAsync();

        // Sender kategorinavnet videre til detaljsiden.
        TempData["NeedCreated"] = "Behovet er registrert.";

        return RedirectToAction(
            "Suggestions",
            "Matching",
            new { needID = need.NeedId });
    }

    // Viser detaljer for et eksisterende behov fra databasen.
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Henter behovet og tilhørende kategori.
        var need = await _context.Needs
            .AsNoTracking()
            .Include(n => n.Category)
            .FirstOrDefaultAsync(n => n.NeedId == id);

        if (need == null)
        {
            return NotFound();
        }

        // Gjør databaseobjektet om til NeedViewModel,
        // som den eksisterende Details.cshtml bruker.
        var model = new NeedViewModel
        {
            Title = need.Title,
            Description = need.Description ?? "",
            CategoryId = need.CategoryId,
            CategoryName = need.Category?.Name,
            Quantity = need.Quantity,
            Unit = need.Unit,
            NeededFrom = need.NeededFrom,
            NeededTo = need.NeededTo,
            Municipality = need.Municipality,
            ContactName = need.ContactName,
            ContactPhone = need.ContactPhone,
            Priority = need.Priority.ToString(),
            Latitude = (double)need.Latitude,
            Longitude = (double)need.Longitude
        };

        ViewData["ExistingNeed"] = true;
        //Detaljsiden vise riktig status uten at du må endre NeedViewModel
        ViewData["NeedStatus"] = need.Status.ToString();
        
        var confirmedQuantity = await _context.Requests
            .AsNoTracking()
            .Where(r => r.Status == RequestStatus.Confirmed
                        && r.Match != null
                        && r.Match.NeedId == id)
            .SumAsync(r => (int?)r.Quantity) ?? 0;

        ViewData["ConfirmedQuantity"] = confirmedQuantity;
        ViewData["RemainingQuantity"] =
            Math.Max(0, need.Quantity - confirmedQuantity);

        // B13: id og neste status brukes av knappen som endrer status
        ViewData["NeedId"] = need.NeedId;
        ViewData["NextStatus"] = NeedStatusFlow.NextStatus(need.Status);

        return View(model);
    }

    // B13: Flytter et behov ett steg fram i statusflyten (ny → vurderes → tildelt → løst).
    // Bare offentlige aktører kan endre status, og serveren sjekker at overgangen er lov,
    // så ingen kan hoppe over steg eller gå bakover ved å sende en egen forespørsel.
    [Authorize(Roles = AppRoles.PublicAuthority)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, NeedStatus newStatus)
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

            var needs = await _context.Needs
                .FromSqlInterpolated(
                    $"SELECT * FROM `Needs` WHERE `NeedId` = {id} FOR UPDATE")
                .ToListAsync();

            var need = needs.SingleOrDefault();

            if (need == null)
            {
                return NotFound();
            }

            if (need.PublicAuthorityId != userId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid
                || !Enum.IsDefined(newStatus)
                || !NeedStatusFlow.CanChange(need.Status, newStatus))
            {
                TempData["StatusError"] =
                    $"Statusen kan ikke endres fra «{NeedStatusFlow.DisplayName(need.Status)}» hit.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (newStatus == NeedStatus.Assigned)
            {
                var confirmedQuantity = await _context.Requests
                    .Where(r => r.Status == RequestStatus.Confirmed
                        && r.Match != null
                        && r.Match.NeedId == id)
                    .SumAsync(r => (int?)r.Quantity) ?? 0;

                if (confirmedQuantity < need.Quantity)
                {
                    TempData["StatusError"] =
                        $"Behovet kan ikke settes til tildelt ennå. " +
                        $"{confirmedQuantity} av {need.Quantity} {need.Unit} er bekreftet.";

                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            need.Status = newStatus;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["StatusMessage"] =
                $"Statusen er endret til «{NeedStatusFlow.DisplayName(newStatus)}».";

            return RedirectToAction(nameof(Details), new { id });
        });
    }
}
