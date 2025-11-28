using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduling.Models;

namespace Scheduling.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ScheduleApiController : ControllerBase
    {
        private readonly SchedulingContext _context;

        public ScheduleApiController(SchedulingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetSchedules()
        {
            var data = _context.Schedules
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.ScheduleId,
                    s.UserId,
                    s.StartTime,
                    s.EndTime,
                    s.Work.WorkName,
                    s.User.UserName
                })
                .ToList();

            // 🔁 讓 StartTime / EndTime 保證是 ISO 格式字串
            var formatted = data.Select(x => new
            {
                x.ScheduleId,
                x.UserId,
                x.WorkName,
                x.UserName,
                startTime = x.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                endTime = x.EndTime.ToString("yyyy-MM-ddTHH:mm:ss")
            });

            return Ok(formatted);
        }


        // ✅ 員工：依使用者ID取得自己的班表
        [HttpGet]
        public IActionResult GetSchedulesByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest("UserId 不正確");

            var data = _context.Schedules
                .Where(s => s.UserId == userId && s.IsActive)
                .Select(s => new
                {
                    s.ScheduleId,
                    s.UserId,
                    s.StartTime,
                    s.EndTime,
                    WorkName = s.Work.WorkName
                })
                .ToList();

            if (!data.Any())
                return NotFound("找不到排班資料");

            return Ok(data);
        }

        // ✅ 管理者：顯示所有一般工作與出差班表
        [HttpGet]
        public IActionResult GetSchedulesByRole(int role)
        {
            // role 可用於未來進階分層 (目前可忽略)
            var data = _context.Schedules
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.ScheduleId,
                    s.UserId,
                    s.StartTime,
                    s.EndTime,
                    WorkName = s.Work.WorkName,
                    UserName = s.User.UserName
                })
                .ToList();

            return Ok(data);
        }

    }
}
