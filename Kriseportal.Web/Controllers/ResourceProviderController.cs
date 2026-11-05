
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Controllers;

// Sara: Egen controller for ressursleverandørportalen.
// Sara: Bare innloggede brukere med rollen ResourceProvider får tilgang.
[Authorize(Roles = AppRoles.ResourceProvider)]
public class ResourceProviderController : Controller
{
    // Sara: Viser ressursleverandørens dashboard.
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
