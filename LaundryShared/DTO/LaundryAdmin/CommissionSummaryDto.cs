namespace LaundryShared.DTO.LaundryAdmin
{
    public class CommissionSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionPaid { get; set; }
        public decimal CommissionDue { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
