using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;
using Kriseportal.Web.Repositories;

namespace Kriseportal.Web.Controllers;

public class NeedController : Controller
{
    private readonly INeedRepository _needRepository;

    public NeedController(INeedRepository needRepository)
    {
        _needRepository = needRepository;
    }
    
    [HttpGet]
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