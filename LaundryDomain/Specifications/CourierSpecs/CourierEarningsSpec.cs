using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    public class CourierEarningsSpec : BaseSpecification<Order, int>
    // Earnings — Delivered orders في period معينة
    {
        public CourierEarningsSpec(string courierId, DateTime? from, DateTime? to)
            : base(o => o.CourierId == courierId &&
                        o.Status == OrderStatus.Delivered &&
                        (from == null || o.DeliveredAt >= from) &&
                        (to == null || o.DeliveredAt <= to))
        {
        }
    }
}
