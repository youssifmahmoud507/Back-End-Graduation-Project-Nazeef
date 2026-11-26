using System.Collections.Generic;

namespace LaundryShared.DTO.Analytics
{
    /// <summary>
    /// إحصائيات السيستم كله
    /// بيتعرض في داشبورد الـ Super Admin بس
    /// </summary>
    public class SystemAnalyticsDto
    {
        /// <summary>إجمالي أرباح السيستم كله</summary>
        public decimal TotalRevenue { get; set; }
        public string RevenueGrowth { get; set; } = "+0%";

        /// <summary>إجمالي عدد الطلبات في السيستم</summary>
        public int TotalOrders { get; set; }
        
        public int OrdersToday { get; set; }
        public string OrdersTodayGrowth { get; set; } = "+0%";

        /// <summary>عدد المغاسل الشغالة</summary>
        public int ActiveLaundries { get; set; }
        public string LaundriesGrowth { get; set; } = "+0";

        /// <summary>عدد اليوزرز الكل</summary>
        public int TotalUsers { get; set; }
        public string UsersGrowth { get; set; } = "+0%";

        public decimal TotalCommissions { get; set; }
        public decimal TotalPendingDebts { get; set; }
        public int TotalSuspiciousActivities { get; set; }

        public decimal HealthScore { get; set; }
        public decimal Uptime { get; set; }
        public decimal AvgRating { get; set; }

        /// <summary>عدد اليوزرز الجدد الشهر ده</summary>
        public int NewUsersThisMonth { get; set; }

        /// <summary>أكتر مغسلة نشاطاً في السيستم</summary>
        public string MostActiveLaundry { get; set; } = string.Empty;

        /// <summary>تقرير الأرباح الشهري للسيستم كله</summary>
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();

        public List<OrderStatusDistributionDto> OrderStatusDistribution { get; set; } = new();
        public List<TopLaundryDto> TopPerformingLaundries { get; set; } = new();
        public List<CityOrderRatioDto> OrdersByCity { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }
}