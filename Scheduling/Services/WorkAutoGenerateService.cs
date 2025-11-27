using Scheduling.Models;
using Scheduling.Helpers;
using Microsoft.EntityFrameworkCore;

public class WorkAutoGenerateService
{
    private readonly SchedulingContext _context;
    private readonly IWebHostEnvironment _env;

    public WorkAutoGenerateService(SchedulingContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task GenerateNextMonthScheduleIfNotExistsAsync()
    {
        var today = DateTime.Today;
        var nextMonth = today.AddMonths(1);
        var firstDay = new DateTime(nextMonth.Year, nextMonth.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        // ✅ 檢查下個月是否已有排班
        bool exists = await _context.Schedules
            .AnyAsync(s => s.ScheduleDate >= DateOnly.FromDateTime(firstDay)
                        && s.ScheduleDate <= DateOnly.FromDateTime(lastDay));

        if (exists) return; // 防止重複執行

        // ✅ 找出一般工作(一)、(二)
        var work1 = await _context.Works.FirstOrDefaultAsync(w => w.WorkName == "一般工作(一)");
        var work2 = await _context.Works.FirstOrDefaultAsync(w => w.WorkName == "一般工作(二)");
        if (work1 == null || work2 == null) return;

        // ✅ 載入假日資料
        string json = await HolidayHelper.LoadHolidaysAsync(nextMonth.Year, _env);
        var holidays = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
        var holidayDates = holidays
            .Where(h => h["是否放假"] == "2")
            .Select(h => DateTime.ParseExact(h["西元日期"], "yyyyMMdd", null).Date)
            .ToHashSet();

        // ✅ 建立班表資料
        var schedules = new List<Schedule>();
        for (DateTime d = firstDay; d <= lastDay; d = d.AddDays(1))
        {
            // 跳過週末與國定假日
            if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;
            if (holidayDates.Contains(d)) continue;

            // 一般工作(一)
            schedules.Add(new Schedule
            {
                WorkId = work1.WorkId,
                ScheduleDate = DateOnly.FromDateTime(d),
                StartTime = d.Add(work1.DefaultStartTime?.ToTimeSpan() ?? new TimeSpan(9, 0, 0)),
                EndTime = d.Add(work1.DefaultEndTime?.ToTimeSpan() ?? new TimeSpan(18, 0, 0)),
                UserId = null,            // ✅ 無人
                CreatedBy = null,         // ✅ 系統自動
                Status = "Active"
            });

            // 一般工作(二)
            schedules.Add(new Schedule
            {
                WorkId = work2.WorkId,
                ScheduleDate = DateOnly.FromDateTime(d),
                StartTime = d.Add(work2.DefaultStartTime?.ToTimeSpan() ?? new TimeSpan(9, 0, 0)),
                EndTime = d.Add(work2.DefaultEndTime?.ToTimeSpan() ?? new TimeSpan(18, 0, 0)),
                UserId = null,
                CreatedBy = null,
                Status = "Active"
            });
        }

        await _context.Schedules.AddRangeAsync(schedules);
        await _context.SaveChangesAsync();
    }
}
