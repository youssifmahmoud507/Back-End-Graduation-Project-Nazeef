using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaundryDomain.Models.OrderModules;

namespace LaundryDomain.Specifications.LaundryAdminSpec
{
    /// <summary>
    /// بتجيب كل الطلبات في السيستم
    /// بتستخدم في تقارير الـ Super Admin بس
    /// </summary>
    public class AllOrdersSpec : BaseSpecification<Order, int>
    {
        public AllOrdersSpec()
            : base(o => true)
        {
            // بنجيب الأيتمز والخدمات مع كل طلب
            AddInclude(o => o.OrderItems);

            // بنرتب من الأحدث للأقدم
            AddOrderByDescending(o => o.CreatedAt);
        }
    }
}
