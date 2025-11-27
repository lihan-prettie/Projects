using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Controllers
{
    public class DashboardController : Controller
    {
        private readonly WorkAutoGenerateService _autoService;

        public DashboardController(WorkAutoGenerateService autoService)
        {
            _autoService = autoService;
        }

        public IActionResult Boss()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        public async Task<IActionResult> Manager()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 2)
            {
                return RedirectToAction("Index", "Login");
            }

            await _autoService.GenerateNextMonthScheduleIfNotExistsAsync();
            return View();
        }

        public async Task<IActionResult> Employee()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                return RedirectToAction("Index", "Login");
            }

            await _autoService.GenerateNextMonthScheduleIfNotExistsAsync();
            return View();
        }
    }
}
