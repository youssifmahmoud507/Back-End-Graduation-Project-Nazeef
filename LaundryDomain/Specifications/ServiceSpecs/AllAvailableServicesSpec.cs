using LaundryDomain.Models.LaundryModules;

namespace LaundryDomain.Specifications.ServiceSpecs
{
    public class AllAvailableServicesSpec : BaseSpecification<Service, int>
    {
        public AllAvailableServicesSpec()
            : base(s => s.IsAvailable)
        {
            AddInclude(s => s.Laundry);
            AddOrderBy(s => s.ServiceName);
        }
    }
}
