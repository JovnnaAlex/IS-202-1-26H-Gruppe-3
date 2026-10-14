using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;

namespace Kriseportal.Web.Controllers;

public class ResourceController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(ResourceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        return View("Details", model);
    }
}
// la til en if statement for modelstate validation Edgar