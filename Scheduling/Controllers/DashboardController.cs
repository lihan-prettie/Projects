using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Boss()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        public IActionResult Manager()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 2)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }
        public IActionResult Employee()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }
    }
}
