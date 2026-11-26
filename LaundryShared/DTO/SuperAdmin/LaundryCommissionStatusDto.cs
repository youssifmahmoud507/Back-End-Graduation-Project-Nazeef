namespace LaundryShared.DTO.SuperAdmin
{
    public class LaundryCommissionStatusDto
    {
        public int LaundryId { get; set; }
        public string LaundryName { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminPhone { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionPaid { get; set; }
        public decimal CommissionDue { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PendingPaymentsCount { get; set; }
    }
}
