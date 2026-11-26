using System;
using LaundryDomain.Models.BundleModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundlesByLaundryIdSpec : BaseSpecification<Bundle, Guid>
    {
        public BundlesByLaundryIdSpec(int laundryId) 
            : base(b => b.LaundryId == laundryId)
        {
            AddInclude(b => b.BundleItems);
            AddOrderByDescending(b => b.CreatedAt);
        }
    }
}
