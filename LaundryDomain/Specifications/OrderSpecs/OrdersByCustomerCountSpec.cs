using LaundryDomain.Models.OrderModules;
using LaundryShared.DTO.Order;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class OrdersByCustomerCountSpec : BaseSpecification<Order, int>
    {
        public OrdersByCustomerCountSpec(string customerId, OrderParams orderParams)
            : base(o => o.CustomerId == customerId)
        {
            if (orderParams.Status.HasValue)
                AddCriteria(o => o.Status == orderParams.Status.Value);
        }
    }
}
