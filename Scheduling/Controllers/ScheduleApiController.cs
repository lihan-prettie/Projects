using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduling.Models;
using Scheduling.Models.DTOs;

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
        // ✅ Boss 新增出差工作
        [HttpPost]
        public IActionResult AddBusinessTrip([FromBody] BusinessTripDto model)
        {
            try
            {
                // 檢查時間是否有效
                if (model.StartTime == DateTime.MinValue || model.EndTime == DateTime.MinValue)
                {
                    return BadRequest(new { success = false, message = "時間格式錯誤，請確認前端是否正確傳入時間。" });
                }

                // ✅ 統一轉成台北時間
                var taipeiZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                var startLocal = TimeZoneInfo.ConvertTime(model.StartTime, taipeiZone);
                var endLocal = TimeZoneInfo.ConvertTime(model.EndTime, taipeiZone);

                // ✅ 確保日期正確
                var workDate = DateOnly.FromDateTime(startLocal);

                // ✅ 建立 Work
                var work = new Work
                {
                    WorkName = model.WorkName,
                    WorkLocation = model.WorkLocation,
                    WorkType = "BusinessTrip",
                    WorkNote = model.WorkNote,
                    DefaultStartTime = TimeOnly.FromDateTime(startLocal),
                    DefaultEndTime = TimeOnly.FromDateTime(endLocal),
                    CreateDate = DateTime.Now,
                    IsActive = true
                };

                _context.Works.Add(work);
                _context.SaveChanges();

                // ✅ 建立 Schedule - 修正重點：必須同時設定 ScheduleDate 和 WorkDate
                var schedule = new Schedule
                {
                    WorkId = work.WorkId,
                    UserId = null, // 出差工作不指定特定員工
                    StartTime = startLocal,
                    EndTime = endLocal,
                    ScheduleDate = workDate, // ⚠️ 關鍵：必須設定 ScheduleDate
                    WorkDate = workDate,     // ⚠️ 關鍵：必須設定 WorkDate
                    IsActive = true
                };

                _context.Schedules.Add(schedule);
                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "出差工作與排班已新增",
                    workDate = workDate.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
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
