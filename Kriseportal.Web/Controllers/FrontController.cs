
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Web.Controllers
{
    public class FrontController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
