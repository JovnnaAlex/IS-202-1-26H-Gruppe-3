using Kriseportal.Web.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;
using Kriseportal.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


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
        public IActionResult Suggestions(int needID)
        {
            var behov = _context.Needs.Find(needID);
            if (behov == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (behov.PublicAuthorityId != userId)
            {
                return Forbid();
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

            if (model.Quantity > ressurs.Quantity
            || model.Quantity > behov.Quantity)
            {
                return BadRequest(
                    "Antallet er større enn ressursen har eller behovet trenger.");
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

            return RedirectToAction("Suggestions", new { needID = model.NeedId });

        }
    }
}
