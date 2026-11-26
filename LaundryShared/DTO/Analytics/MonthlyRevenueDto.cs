namespace LaundryShared.DTO.Analytics
{
    /// <summary>
    /// أرباح كل شهر
    /// بيتعرض في الـ Chart بتاع الداشبورد
    /// </summary>
    public class MonthlyRevenueDto
    {
        /// <summary>اسم الشهر (مثال: January 2026)</summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>إجمالي الأرباح في الشهر ده</summary>
        public decimal Revenue { get; set; }

        /// <summary>عدد الطلبات في الشهر ده</summary>
        public int Orders { get; set; }

        /// <summary>صافي أرباح المنصة (العمولة)</summary>
        public decimal Profit { get; set; }
    }
}