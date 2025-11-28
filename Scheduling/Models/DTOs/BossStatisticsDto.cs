namespace Scheduling.Models.DTOs
{
    public class BossStatisticsDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int MonthlyCount { get; set; }
        public int YearlyCount { get; set; }
    }
}
