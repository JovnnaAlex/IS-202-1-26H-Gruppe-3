using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}