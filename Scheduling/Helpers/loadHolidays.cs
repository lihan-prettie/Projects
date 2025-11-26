namespace Scheduling.Helpers
{
    public static class HolidayHelper
    {
        public static async Task<string> LoadHolidaysAsync(int year,IWebHostEnvironment env) {
            string fileName = $"holidays{year}.json";
            string path = Path.Combine(env.WebRootPath,"js",fileName);
            if (!File.Exists(path)) return "[]";
            using var fileStream = new FileStream(path,FileMode.Open,FileAccess.Read);
            using var reader = new StreamReader(fileStream);
            string json =  await reader.ReadToEndAsync();
            return json;
        }
    }
}
