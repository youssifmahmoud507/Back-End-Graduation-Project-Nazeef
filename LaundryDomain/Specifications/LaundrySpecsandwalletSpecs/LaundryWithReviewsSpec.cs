using LaundryDomain.Models.LaundryModules;

namespace LaundryDomain.Specifications.LaundrySpecs
{
    public class LaundryWithReviewsSpec : BaseSpecification<Laundry, int>
    {
        public LaundryWithReviewsSpec(int laundryId)
            : base(l => l.Id == laundryId)
        {
            AddInclude(l => l.Reviews);
        }
    }
}
