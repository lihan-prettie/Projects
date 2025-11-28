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
                    WorkName = s.Work.WorkName,
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

        // ✅ Boss 修改使用者角色
        [HttpPost]
        public IActionResult UpdateUserRole([FromBody] User updateModel)
        {
            if (updateModel == null || updateModel.UserId <= 0)
                return BadRequest("無效的使用者資料");

            var user = _context.Users.FirstOrDefault(u => u.UserId == updateModel.UserId);
            if (user == null) return NotFound("找不到使用者");

            user.RoleId = updateModel.RoleId;
            user.UpdateDate = DateTime.Now;
            _context.SaveChanges();

            return Ok(new { success = true, message = "角色更新成功" });
        }

        // ✅ Boss 查詢員工清單（給修改角色用）
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users
                .Select(u => new { u.UserId, u.UserName, u.RoleId })
                .ToList();

            return Ok(users);
        }
    }
}
