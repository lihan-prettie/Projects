using Microsoft.AspNetCore.Mvc;
using Scheduling.Models;

namespace Scheduling.Controllers
{
    public class ScheduleController : Controller
    {

        private readonly SchedulingContext _context;
        public ScheduleController(SchedulingContext context)
        {
            _context = context;
        }
        
        //GET : Schedule/AddWorkPartial
        [HttpGet]
        public IActionResult EditWorkPartial(int workId,DateTime date)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int? roleId = HttpContext.Session.GetInt32("RoleId");


            return PartialView("~/Views/ParticalView/EditWorkPartial.cshtml");
        }
    }
}