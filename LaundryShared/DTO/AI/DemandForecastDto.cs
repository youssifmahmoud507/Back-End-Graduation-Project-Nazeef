using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.AI
{
    public class DemandForecastDto
    {
        public int ExpectedOrdersNextWeek { get; set; }
        public decimal ExpectedRevenueNextWeek { get; set; }
        public DemandLevel DemandLevel { get; set; }
        public string? Insights { get; set; }
        public List<DailyForecastDto> DailyBreakdown { get; set; } = new();
    }
}
