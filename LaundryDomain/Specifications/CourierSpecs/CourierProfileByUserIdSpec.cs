using LaundryDomain.Models.CourierModules;

namespace LaundryDomain.Specifications.CourierSpecs
{
    public class CourierProfileByUserIdSpec : BaseSpecification<CourierProfile, int>
    // CourierProfile specs
    {
        public CourierProfileByUserIdSpec(string courierId)
            : base(cp => cp.CourierId == courierId)
        {
            AddInclude(cp => cp.Courier);
            AddInclude(cp => cp.Laundry);
        }
    }
}
