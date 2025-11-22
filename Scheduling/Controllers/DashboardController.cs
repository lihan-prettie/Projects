using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Boss() => View();
        public IActionResult Manager() => View();
        public IActionResult Employee() => View();

    }
}
