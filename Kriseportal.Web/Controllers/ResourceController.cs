using Microsoft.AspNetCore.Mvc;
using Kriseportal.Web.Models;
using Kriseportal.Web.Repositories;

namespace Kriseportal.Web.Controllers;

public class ResourceController : Controller
{
    private readonly IResourceRepository _resourceRepository;

    public ResourceController(IResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }
    
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