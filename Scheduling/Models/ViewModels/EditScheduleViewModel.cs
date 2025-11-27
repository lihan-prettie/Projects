namespace Scheduling.Models.ViewModels
{
    public class EditScheduleViewModel
    {
        public int ScheduleId { get; set; }
        public string WorkName { get; set; }
        public string ScheduleDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string WorkLocation { get; set; }
        public string WorkNote { get; set; }
        public string Status { get; set; }
        public int CreatedBy { get; set; }
        public int? UserId { get; set; }
        public bool IsEditable { get; set; }
    }
}
