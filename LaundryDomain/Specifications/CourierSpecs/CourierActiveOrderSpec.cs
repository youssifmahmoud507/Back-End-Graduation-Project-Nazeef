using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    // Active order للكورير (اللي بيشتغل عليها دلوقتي)
    public class CourierActiveOrderSpec : BaseSpecification<Order, int>
    {
        public CourierActiveOrderSpec(string courierId)
            : base(o => o.CourierId == courierId &&
                        (o.Status == OrderStatus.ReadyForPickup ||
                         o.Status == OrderStatus.PickedUp))
        {
            AddInclude(o => o.Customer);
            AddInclude(o => o.Laundry);
        }
    }
}
