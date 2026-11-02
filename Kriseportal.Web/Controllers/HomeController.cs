using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers;

public class HomeController : Controller
{
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