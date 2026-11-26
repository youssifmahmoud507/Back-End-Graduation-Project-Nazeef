using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class OrderHistoryCountSpec : BaseSpecification<Order, int>
    {
        public OrderHistoryCountSpec(string customerId)
            : base(o => o.CustomerId == customerId &&
                        o.Status == OrderStatus.Delivered ||
                        o.Status == OrderStatus.Cancelled)
        {
        }
    }
}
