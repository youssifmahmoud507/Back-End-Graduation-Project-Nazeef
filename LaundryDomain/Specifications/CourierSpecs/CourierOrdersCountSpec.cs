using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;
using System.Linq.Expressions;

namespace LaundryDomain.Specifications.CourierSpecs
{
    // Count spec
    public class CourierOrdersCountSpec : BaseSpecification<Order, int>
    {
        public CourierOrdersCountSpec(string courierId, CourierOrderFilter filter)
            : base(GetPredicate(courierId, filter))
        {
        }

        private static Expression<Func<Order, bool>> GetPredicate(string courierId, CourierOrderFilter filter)
        {
            return filter switch
            {
                CourierOrderFilter.Pending => o => o.CourierId == courierId && o.Status == OrderStatus.ReadyForPickup,
                CourierOrderFilter.InProgress => o => o.CourierId == courierId && o.Status == OrderStatus.PickedUp,
                CourierOrderFilter.Completed => o => o.CourierId == courierId && o.Status == OrderStatus.Delivered,
                _ => o => o.CourierId == courierId && o.Status != OrderStatus.Cancelled
            };
        }
    }
}
