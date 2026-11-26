using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    public class CourierDeliveryHistoryCountSpec : BaseSpecification<Order, int>
    {
        public CourierDeliveryHistoryCountSpec(string courierId)
            : base(o => o.CourierId == courierId &&
                        (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Cancelled))
        {
        }
    }
}
