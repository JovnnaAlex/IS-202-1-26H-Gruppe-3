using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
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
