using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.CourierSpecs
{
    public class CourierOrdersSpec : BaseSpecification<Order, int>
    {
        public CourierOrdersSpec(string courierId, CourierOrderFilter filter, int skip, int take)
            : base(GetPredicate(courierId, filter))
        {
            AddInclude(o => o.Customer);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Payment);
            AddInclude("OrderItems.Service"); // ThenInclude-style: get Service name per item
            AddOrderByDescending(o => o.PickupTime);
            ApplyPaging(skip, take);
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
