using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;

namespace Kriseportal.Web.Controllers;

public class NeedController : Controller
{
    [HttpGet]
    // Viser oversikten over behov
    public IActionResult Index()
    {
        return View();
    }
    //Tar deg til opprett behov siden
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(NeedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return View("Details", model);
    }
}

// la til en if statement for modelstate validation Edgar