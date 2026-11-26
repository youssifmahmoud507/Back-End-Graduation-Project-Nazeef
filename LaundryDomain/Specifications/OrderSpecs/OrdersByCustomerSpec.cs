using LaundryDomain.Models.OrderModules;
using LaundryShared.DTO.Order;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class OrdersByCustomerSpec : BaseSpecification<Order, int>
    {
        public OrdersByCustomerSpec(string customerId, OrderParams orderParams)
            : base(o => o.CustomerId == customerId)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.Payment);
            AddInclude(o => o.Coupon);

            // Filter by Status
            if (orderParams.Status.HasValue)
                AddCriteria(o => o.Status == orderParams.Status.Value);

            // Order by latest first
            AddOrderByDescending(o => o.CreatedAt);

            // Pagination
            ApplyPaging(
                (orderParams.PageIndex - 1) * orderParams.PageSize,
                orderParams.PageSize
            );
        }
    }
}
