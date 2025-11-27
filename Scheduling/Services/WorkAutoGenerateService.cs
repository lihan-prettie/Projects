using Scheduling.Helpers;
using Scheduling.Models;

namespace Scheduling.Services
{
    public class WorkAutoGenerateService
    {
        private readonly SchedulingContext _context;
        private readonly IWebHostEnvironment _env;

        public WorkAutoGenerateService(SchedulingContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task GenerateNextMonthWorkIfNotExistsAsync()
        {
            var today = DateTime.Today;

            // 下個月的第一天與最後一天（都用 Date 層級避免時間差）
            var nextMonthFirstDay = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            var nextMonthLastDay = nextMonthFirstDay.AddMonths(1).AddDays(-1);

            // 建立時間（你原本要「前一個月的一號」）：
            var createDate = new DateTime(today.Year, today.Month, 1);

            // 防重邏輯最好依「要生成的月份」判斷，而不是今天月份
            // 例如：只要已存在「系統自動生成」且 CreateDate == 本月一號 的資料，就不重複生成
            bool alreadyGenerated = _context.Works.Any(w =>
                w.WorkNote == "系統自動生成" &&
                w.CreateDate.HasValue &&
                w.CreateDate.Value.Date == createDate.Date);

            if (alreadyGenerated) return;

            // 取得下個年度的假日（或直接取下個月所屬年就好）
            var holidays = await HolidayHelper.GetHolidayDatesAsync(nextMonthFirstDay.Year, _env);

            // 為了加速判斷、避免時分秒干擾，用 Date 轉成 HashSet
            var holidaySet = new HashSet<DateTime>(holidays.Select(h => h.Date));

            var workList = new List<Work>();

            for (var d = nextMonthFirstDay; d <= nextMonthLastDay; d = d.AddDays(1))
            {
                var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                if (isWeekend) continue;
                if (holidaySet.Contains(d.Date)) continue;

                for (int i = 1; i <= 2; i++)
                {
                    workList.Add(new Work
                    {
                        WorkName = $"一般工作({i})",
                        WorkType = "Normal Work",
                        WorkNote = "系統自動生成",
                        IsActive = true,
                        DefaultStartTime = new TimeOnly(9, 0),
                        DefaultEndTime = new TimeOnly(18, 0),
                        CreateDate = createDate,
                        WorkLocation = "台北市中山區建國北路一段96號",
                    });
                }
            }

            if (workList.Count == 0) return;

            _context.Works.AddRange(workList);
            await _context.SaveChangesAsync();
        }
    }
}
