using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
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