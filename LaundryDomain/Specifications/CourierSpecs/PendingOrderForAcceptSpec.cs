using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    // أوردر Pending مش متعيّن لحد — للـ Accept
    public class PendingOrderForAcceptSpec : BaseSpecification<Order, int>
    {
        public PendingOrderForAcceptSpec(int orderId)
            : base(o => o.Id == orderId &&
                        (o.Status == OrderStatus.ReadyForPickup || o.Status == OrderStatus.Accepted) &&
                        o.CourierId == null)
        {
            AddInclude(o => o.Customer);
            AddInclude(o => o.Laundry);
        }
    }
}
