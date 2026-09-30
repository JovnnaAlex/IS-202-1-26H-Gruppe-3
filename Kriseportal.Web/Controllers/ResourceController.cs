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
        return View("Details", model);
    }
}