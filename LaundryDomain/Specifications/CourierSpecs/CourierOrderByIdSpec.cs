using LaundryDomain.Models.OrderModules;

namespace LaundryDomain.Specifications.CourierSpecs
{
    // جلب أوردر واحد بـ ID مع كل التفاصيل
    public class CourierOrderByIdSpec : BaseSpecification<Order, int>
    {
        public CourierOrderByIdSpec(int orderId)
            : base(o => o.Id == orderId)
        {
            AddInclude(o => o.Customer);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Payment);
        }
    }
}
