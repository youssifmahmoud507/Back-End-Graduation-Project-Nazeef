using System;
using LaundryDomain.Models.BundleModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class ActiveBundlesByLaundrySpec : BaseSpecification<Bundle, Guid>
    {
        public ActiveBundlesByLaundrySpec(int laundryId) 
            : base(b => b.Status == BundleStatus.Active
                     && (b.ExpiryDate == null || b.ExpiryDate > DateTime.UtcNow) 
                     && ((b.LaundryId == laundryId) || (b.OwnerType == BundleOwnerType.Platform && b.LaundryId == null)))
        {
            AddInclude(b => b.BundleItems);
            AddInclude("BundleItems.Service");
            AddOrderBy(b => b.DisplayOrder);
        }
    }
}