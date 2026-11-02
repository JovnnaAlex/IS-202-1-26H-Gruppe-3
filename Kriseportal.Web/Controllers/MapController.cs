using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Models.Map;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Controllers;

public class MapController : Controller
{
    private readonly ApplicationDbContext _context;

    public MapController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Viser situasjonskartet
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // Sender behov og ressurser som JSON, slik at kartet kan tegne dem
    [HttpGet]
    public async Task<IActionResult> Data()
    {
        // Løste behov vises ikke på kartet
        var needs = await _context.Needs
            .AsNoTracking()
            .Where(n => n.Status != NeedStatus.Resolved)
            .Select(n => new
            {
                n.NeedId,
                n.Title,
                Category = n.Category!.Name,
                n.Priority,
                n.Status,
                n.Quantity,
                n.Unit,
                n.Latitude,
                n.Longitude,
                n.Municipality,
                n.ContactName,
                n.ContactPhone
            })
            .ToListAsync();

        var resources = await _context.Resources
            .AsNoTracking()
            .Select(r => new
            {
                r.ResourceId,
                r.Name,
                Category = r.Category!.Name,
                r.Availability,
                r.Quantity,
                r.Unit,
                r.Latitude,
                r.Longitude,
                r.Municipality,
                r.ContactName,
                r.ContactPhone
            })
            .ToListAsync();

        // Gjør begge om til det felles kartformatet
        var points = needs
            .Select(n => new MapPoint(
                n.NeedId, "need", n.Title, n.Category, n.Priority.ToString(), n.Status.ToString(),
                n.Quantity, n.Unit, (double)n.Latitude, (double)n.Longitude,
                n.Municipality, n.ContactName, n.ContactPhone))
            .Concat(resources.Select(r => new MapPoint(
                r.ResourceId, "resource", r.Name, r.Category, null, r.Availability.ToString(),
                r.Quantity, r.Unit, (double)r.Latitude, (double)r.Longitude,
                r.Municipality, r.ContactName, r.ContactPhone)))
            .ToList();

        return Json(new { updatedAt = DateTime.UtcNow, points });
    }
}