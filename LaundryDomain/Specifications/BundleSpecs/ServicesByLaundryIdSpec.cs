using LaundryDomain.Models.LaundryModules;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class ServicesByLaundryIdSpec : BaseSpecification<Service, int>
    {
        public ServicesByLaundryIdSpec(int laundryId) 
            : base(s => s.LaundryId == laundryId && s.IsAvailable == true)
        {
        }
    }
}
