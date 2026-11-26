using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.ReviewSpecs
{
    public class ReviewsByCustomerSpec : BaseSpecification<Review, int>
    {
        public ReviewsByCustomerSpec(string customerId)
            : base(r => r.CustomerId == customerId)
        {
            AddInclude(r => r.Laundry);
            AddOrderByDescending(r => r.CreatedAt);
        }
    }
}
