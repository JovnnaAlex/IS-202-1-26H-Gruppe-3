using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
    // B12: Bare innloggede brukere har en profil og innstillinger
    [Authorize]
    public class SettingsController : Controller
    {
        // Frontend: viser innstillingssiden.
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
