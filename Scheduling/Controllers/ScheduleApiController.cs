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
        public async Task<IActionResult> GetSchedules()
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");

            var schedules = await _context.Schedules
                .Include(s => s.Work)
                .ToListAsync();

            var events = schedules.Select(s => new
            {
                id = s.ScheduleId,
                title = s.Work.WorkName,
                start = s.ScheduleDate.ToString("yyyy-MM-dd"),
                color =
                    s.UserId == null ? "#B0B0B0" :                        // 灰色 → 無人
                    s.UserId == currentUserId ? "#FF7043" : "#FFD54F",    // 橘紅 / 黃
                extendedProps = new
                {
                    userId = s.UserId,
                    workId = s.WorkId,
                    status = s.Status,
                    startTime = s.StartTime.ToString("HH:mm"),
                    endTime = s.EndTime.ToString("HH:mm")
                }
            });

            return Ok(events);
        }

    }
}
