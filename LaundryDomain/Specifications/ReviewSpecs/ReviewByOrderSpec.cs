using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.ReviewSpecs
{
    public class ReviewByOrderSpec : BaseSpecification<Review, int>
    {
        public ReviewByOrderSpec(int orderId, string customerId)
            : base(r => r.OrderId == orderId && r.CustomerId == customerId)
        {
            AddInclude(r => r.Laundry);
        }
    }
}
