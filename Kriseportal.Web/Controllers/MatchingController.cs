using Kriseportal.Web.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Models;


namespace Kriseportal.Web.Controllers
{
    public class MatchingController : Controller
    {
    private readonly ApplicationDbContext _context;
    public MatchingController(ApplicationDbContext context)
    {
        _context = context;
    }
    
        [HttpGet]
        public IActionResult Suggestions(int needID)
        {
            var behov = _context.Needs.Find(needID);
            if (behov == null)
            {
                return NotFound();
            }
                var Ressurser = _context.Resources.
                 Where(r => r.Availability == ResourceAvailability.Available
                 && r.CategoryId == behov.CategoryId).ToList();

            var model = new MatchingSuggestionsViewModel
            {
                NeedId = needID,
                Resources = Ressurser
            };

            return View("Suggestions", model);
        }
    }
}
