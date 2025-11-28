using Scheduling.Models;
using Scheduling.Models.DTOs;

namespace Scheduling.Services
{
    public class ScheduleService
    {
        private readonly SchedulingContext _context;

        public ScheduleService(SchedulingContext context)
        {
            _context = context;
        }

        // ✅ 1️取得所有員工全年出勤
        public IEnumerable<object> GetAllSchedulesForYear(int year)
        {
            var data = _context.Schedules
                .Where(s => s.StartTime.Year == year && s.IsActive)
                .GroupBy(s => new { s.WorkId, s.StartTime.Date }) // 🧠 以班別 + 日期分組
                .Select(g => new
                {
                    WorkId = g.Key.WorkId,
                    WorkDate = g.Key.Date,
                    WorkName = g.First().Work.WorkName,
                    UserList = g.Select(x => new { x.UserId, x.User.UserName }).ToList()
                })
                .ToList();

            return data;
        }




        // ✅ 2️⃣ 取得月統計 / 年統計
        public IEnumerable<BossStatisticsDto> GetMonthlyStatistics(int year, int month)
        {
            var query = _context.Schedules
                .Where(s => s.StartTime.Year == year && s.IsActive && s.UserId != null)
                .GroupBy(s => s.UserId)
                .Select(g => new BossStatisticsDto
                {
                    UserId = g.Key.Value,
                    UserName = _context.Users.FirstOrDefault(u => u.UserId == g.Key).UserName ?? "未指派",
                    MonthlyCount = g.Count(s => s.StartTime.Month == month),
                    YearlyCount = g.Count()
                })
                .OrderByDescending(x => x.MonthlyCount)
                .ToList();

            return query;
        }

    }
}
