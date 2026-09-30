using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;

namespace Kriseportal.Web.Controllers;

public class NeedController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(NeedViewModel model)
    {
        return View("Details", model);
    }
}