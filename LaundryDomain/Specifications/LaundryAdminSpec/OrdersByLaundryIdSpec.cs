using LaundryDomain.Models.OrderModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.LaundryAdminIdSpec
{
    /// <summary>
    /// بتجيب كل الطلبات الواردة على مغسلة معينة
    /// بتجيب معاها بيانات العميل والأيتمز والخدمات
    /// </summary>
    public class OrdersByLaundryIdSpec : BaseSpecification<Order, int>
    {
        public OrdersByLaundryIdSpec(int laundryId)
            : base(o => o.LaundryId == laundryId)
        {
            // بنجيب بيانات العميل عشان نعرض اسمه وتليفونه
            AddInclude(o => o.Customer);

            // بنجيب الأيتمز عشان نعرض تفاصيل الطلب
            AddInclude(o => o.OrderItems);

            // بنرتب من الأحدث للأقدم
            AddOrderByDescending(o => o.CreatedAt);
        }
    }
}
