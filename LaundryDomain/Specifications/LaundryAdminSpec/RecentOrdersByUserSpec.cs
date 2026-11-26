using LaundryDomain.Models.OrderModules;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryDomain.Specifications.LaundryAdminSpec
{
    /// <summary>
    /// بتجيب أوردرات يوزر معين في آخر 24 ساعة
    /// بتستخدم في الـ Fraud Detection
    /// </summary>
    public class RecentOrdersByUserSpec : BaseSpecification<Order, int>
    {
        public RecentOrdersByUserSpec(string userId)
            : base(o => o.CustomerId.ToString() == userId &&
                        o.CreatedAt >= DateTime.UtcNow.AddHours(-24))
        {
            AddOrderByDescending(o => o.CreatedAt);
        }
    }
}
