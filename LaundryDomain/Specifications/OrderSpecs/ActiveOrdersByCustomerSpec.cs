using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class ActiveOrdersByCustomerSpec : BaseSpecification<Order, int>
    {
        public ActiveOrdersByCustomerSpec(string customerId)
            : base(o => o.CustomerId == customerId &&
                        o.Status != OrderStatus.Delivered &&
                        o.Status != OrderStatus.Cancelled)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.Payment);
            AddInclude(o => o.Coupon);

            AddOrderByDescending(o => o.CreatedAt);
        }
    }
}
