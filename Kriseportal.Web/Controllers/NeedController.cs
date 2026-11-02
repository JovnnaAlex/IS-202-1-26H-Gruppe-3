using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;
using Kriseportal.Web.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace Kriseportal.Web.Controllers;

public class NeedController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public NeedController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

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

        // Sender kategorinavnet videre til detaljsiden.
        model.CategoryName = category.Name;

        return View("Details", model);
    }
}
