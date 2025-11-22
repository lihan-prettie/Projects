using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
