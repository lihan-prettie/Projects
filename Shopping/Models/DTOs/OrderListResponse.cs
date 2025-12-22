namespace Shopping.Models.DTOs
{
    public class OrderListResponse
    {
        
        public int OrderId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }

    }
}