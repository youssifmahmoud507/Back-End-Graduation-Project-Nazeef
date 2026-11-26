using LaundryDomain.Models.OrderModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundryAdminIdSpec
{
    /// <summary>
    /// بتجيب طلب معين بكل تفاصيله
    /// بتستخدم لما صاحب المغسلة يفتح تفاصيل طلب معين
    /// </summary>
    public class OrderByIdWithDetailsSpec : BaseSpecification<Order, int>
    {
        public OrderByIdWithDetailsSpec(int orderId)
            : base(o => o.Id == orderId)
        {
            // بيانات العميل
            AddInclude(o => o.Customer);

            // الأيتمز + الخدمة الخاصة بكل أيتم
            AddInclude(o => o.OrderItems);

            // بيانات الدفع
            AddInclude(o => o.Payment);
        }
    }
}
