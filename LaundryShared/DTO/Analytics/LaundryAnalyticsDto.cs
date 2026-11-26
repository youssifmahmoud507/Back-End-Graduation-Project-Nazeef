using System.Collections.Generic;

namespace LaundryShared.DTO.Analytics
{
    /// <summary>
    /// تقرير الأرباح والطلبات بتاع صاحب المغسلة
    /// بيتعرض في داشبورد الـ Laundry Admin
    /// </summary>
    public class LaundryAnalyticsDto
    {
        /// <summary>إجمالي الأرباح</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>إجمالي عدد الطلبات</summary>
        public int TotalOrders { get; set; }

        /// <summary>عدد الطلبات الجديدة النهارده</summary>
        public int TodayOrders { get; set; }

        /// <summary>عدد الطلبات اللي لسه شغالة</summary>
        public int ActiveOrders { get; set; }

        /// <summary>متوسط التقييم</summary>
        public double AverageRating { get; set; }

        /// <summary>أكتر خدمة مطلوبة</summary>
        public string MostRequestedService { get; set; } = string.Empty;

        /// <summary>تقرير الأرباح الشهري</summary>
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    }
}