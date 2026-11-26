using LaundryDomain.Models.OrderModules;
using LaundryShared.DTO.Order;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class OrderHistorySpec : BaseSpecification<Order, int>
    {
        public OrderHistorySpec(string customerId, OrderParams orderParams)
            : base(o => o.CustomerId == customerId &&
                        o.Status == OrderStatus.Delivered ||
                        o.Status == OrderStatus.Cancelled)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.Payment);
            AddInclude(o => o.Coupon);

            AddOrderByDescending(o => o.CreatedAt);

            ApplyPaging(
                (orderParams.PageIndex - 1) * orderParams.PageSize,
                orderParams.PageSize
            );
        }
    }
}
