using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
    // B12: Bare innloggede brukere har en profil og innstillinger
    [Authorize]
    public class ProfileController : Controller
    {
        // Viser profilsiden
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Viser siden for redigering av profil
        [HttpGet]
        public IActionResult Edit()
        {
            return View();
        }
    }
}