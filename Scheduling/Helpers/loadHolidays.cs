using System.Text.Json;

namespace Scheduling.Helpers
{
    public static class HolidayHelper
    {
        // ✅ 讀取指定年份的 holiday JSON 檔案內容
        public static async Task<string> LoadHolidaysAsync(int year, IWebHostEnvironment env)
        {
            string fileName = $"holidays{year}.json";
            string path = Path.Combine(env.WebRootPath, "js", fileName);
            if (!File.Exists(path)) return "[]";
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(fileStream);
            string json = await reader.ReadToEndAsync();
            return json;
        }

        // ✅ 轉換 JSON → List<DateTime>
        public static async Task<List<DateTime>> GetHolidayDatesAsync(int year, IWebHostEnvironment env)
        {
            string json = await LoadHolidaysAsync(year, env);
            var holidays = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);

            var dateList = new List<DateTime>();
            foreach (var h in holidays)
            {
                if (h.TryGetValue("西元日期", out string dateStr) &&
                    h.TryGetValue("是否放假", out string isHoliday) &&
                    isHoliday == "2" &&
                    DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    dateList.Add(date);
                }
            }
            return dateList;
        }
    }
}
