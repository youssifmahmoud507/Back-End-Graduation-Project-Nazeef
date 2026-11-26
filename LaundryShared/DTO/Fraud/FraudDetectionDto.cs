using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryShared.DTO.Fraud
{
    /// <summary>
    /// بيانات العملية اللي هيتم تحليلها
    /// عشان نكتشف لو في حاجة مشبوهة
    /// </summary>
    public class FraudDetectionDto
    {
        /// <summary>الـ ID بتاع اليوزر</summary>
        public string UserId { get; set; }

        /// <summary>نوع العملية (Order / Payment / Login)</summary>
        public string ActionType { get; set; }

        /// <summary>الـ IP Address بتاع اليوزر</summary>
        public string IpAddress { get; set; }

        /// <summary>المبلغ لو عملية دفع</summary>
        public decimal? Amount { get; set; }

        /// <summary>عدد المحاولات الفاشلة</summary>
        public int FailedAttempts { get; set; }

        /// <summary>وقت العملية</summary>
        public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    }
}
