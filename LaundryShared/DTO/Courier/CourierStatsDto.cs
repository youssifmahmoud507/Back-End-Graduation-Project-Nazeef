namespace LaundryShared.DTO.Courier
{
    public class CourierStatsDto
    {
    // Stats & Earnings
        public int TotalCompletedOrders { get; set; }
        public int TotalCancellations { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TodayEarnings { get; set; }
        public decimal WeekEarnings { get; set; }
        public decimal MonthEarnings { get; set; }
        public double CompletionRate { get; set; } // percentage
    }
}
