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

        // ✅ Boss 新增出差工作
        [HttpPost]
        public IActionResult AddBusinessTrip([FromBody] Work work)
        {
            try
            {
                if (work == null || string.IsNullOrWhiteSpace(work.WorkName))
                    return BadRequest("資料不完整");

                work.WorkType = "BusinessTrip";
                work.WorkLocation = work.WorkLocation ?? "未指定地點";
                work.DefaultStartTime = new TimeOnly(9, 0, 0);
                work.DefaultEndTime = new TimeOnly(18, 0, 0);
                work.CreateDate = DateTime.Now;
                work.IsActive = true;
                _context.Works.Add(work);
                _context.SaveChanges();

                return Ok(new { success = true, message = "出差工作已新增" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

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

        [HttpGet]
        public IActionResult GetSchedulesByRole(int role)
        {
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
