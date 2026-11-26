using System;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Specifications;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class ExpiredBundlesSpec : BaseSpecification<Bundle, Guid>
    {
        public ExpiredBundlesSpec()
            : base(b => b.Status == BundleStatus.Active && b.ExpiryDate != null && b.ExpiryDate < DateTime.UtcNow)
        {
        }
    }
}