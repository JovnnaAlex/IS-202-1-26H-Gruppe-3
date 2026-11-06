using Kriseportal.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers;

public class HomeController : Controller
{
    // B12: Beredskapsoversikten er bare for offentlige aktører
    [Authorize(Roles = AppRoles.PublicAuthority)]
    public IActionResult Index()
    {
        return View();
    }

    // Enkel feilside i produksjon (brukes av UseExceptionHandler i Program.cs)
    [Route("Home/Error")]
    public IActionResult Error()
    {
        return Content("Noe gikk galt. Prøv igjen senere.");
    }
}