using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Controllers
{
    public class ScheduleController : Controller
    {
        [HttpGet]
        public IActionResult AddWorkPartial(DateTime date)
        {
            ViewData["SelectedDate"] = date.ToString("yyyy-MM-dd");
            return PartialView("~/Views/ParticalView/_AddWorkPartial.cshtml");
        }
    }
}
