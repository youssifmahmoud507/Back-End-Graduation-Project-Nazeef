using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    // Delivery history للكورير
    public class CourierDeliveryHistorySpec : BaseSpecification<Order, int>
    {
        public CourierDeliveryHistorySpec(string courierId, int skip, int take)
            : base(o => o.CourierId == courierId &&
                        (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Cancelled))
        {
            AddOrderByDescending(o => o.DeliveredAt ?? o.UpdatedAt);
            ApplyPaging(skip, take);
        }
    }
}
