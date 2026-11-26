using LaundryDomain.Models.BundleModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class PlatformBundlesSpec : BaseSpecification<Bundle, Guid>
    {
        public PlatformBundlesSpec()
            : base(b => b.OwnerType == BundleOwnerType.Platform
                     && b.Status == BundleStatus.Active
                     && (b.ExpiryDate == null || b.ExpiryDate > DateTime.UtcNow))
        {
            AddInclude(b => b.BundleItems);
            AddInclude("BundleItems.Service");
            AddOrderBy(b => b.DisplayOrder);
        }
    }
}