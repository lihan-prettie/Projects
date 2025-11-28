using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduling.Models;
using Scheduling.Services;

namespace Scheduling.Controllers
{
    public class BossController : Controller
    {
        private readonly ScheduleService _scheduleService;
        private readonly SchedulingContext _context;


        public BossController(ScheduleService scheduleService, SchedulingContext context)
        {
            _scheduleService = scheduleService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ✅ Boss 日曆資料來源
        [HttpGet]
        public IActionResult GetAllSchedules(int year)
        {
            var data = _context.Schedules
                .Where(s => s.StartTime.Year == year && s.IsActive)
                .Select(s => new
                {
                    s.ScheduleId,
                    s.UserId,
                    s.StartTime,
                    s.EndTime,
                    WorkName = s.Work.WorkName,       // ✅ 加這行
                    UserName = s.User != null ? s.User.UserName : null
                })
                .ToList();

            return Ok(data);
        }


        // ✅ Boss 統計資料
        [HttpGet]
        public IActionResult GetMonthlyStats(int year, int month)
        {
            var data = _scheduleService.GetMonthlyStatistics(year, month);
            return Json(data);
        }

        [HttpGet]
        public IActionResult GetPeopleByWork(int scheduleId)
        {
            var workId = _context.Schedules
                .Where(s => s.ScheduleId == scheduleId)
                .Select(s => s.WorkId)
                .FirstOrDefault();

            if (workId == 0)
                return Json(new { message = "找不到對應工作" });

            var people = _context.Schedules
                .Where(s => s.WorkId == workId && s.IsActive)
                .Select(s => new
                {
                    s.UserId,
                    UserName = s.User.UserName
                })
                .ToList();

            return Json(people);
        }
        
    }
}
