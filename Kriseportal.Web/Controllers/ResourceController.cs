
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;

// Sara: Lagt til for å hente ressurser fra databasen.
using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Controllers;

public class ResourceController : Controller
{
    // Sara: Kobler ResourceController til databasen.
    private readonly ApplicationDbContext _context;

    public ResourceController(ApplicationDbContext context)
    {
        _context = context;
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

    // Sara: Åpner den nye siden for å sende forespørsel.
    // Foreløpig er dette bare frontend, uten lagring i databasen.
    [HttpGet]
    public IActionResult SendRequest()
    {
        return View();
    }

    // Eksisterende funksjon for ressursregistrering.
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Eksisterende funksjon for registreringsbekreftelse.
    [HttpPost]
    public IActionResult Create(ResourceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return View("Details", model);
    }
}
// la til en if statement for modelstate validation Edgar
