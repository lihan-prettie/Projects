namespace Scheduling.Models.DTOs
{
    public class BusinessTripDto
    {
        public string WorkName { get; set; }
        public string WorkLocation { get; set; }
        public string WorkNote { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int UserId { get; set; }
    }
}
