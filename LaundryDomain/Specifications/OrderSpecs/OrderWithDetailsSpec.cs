using LaundryDomain.Models.OrderModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class OrderWithDetailsSpec : BaseSpecification<Order, int>
    {
        // جلب أوردر معين بكل تفاصيله
        public OrderWithDetailsSpec(int orderId, string customerId)
            : base(o => o.Id == orderId && o.CustomerId == customerId)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Payment);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.Coupon);
            AddInclude(o => o.Courier);
        }
    }
}
