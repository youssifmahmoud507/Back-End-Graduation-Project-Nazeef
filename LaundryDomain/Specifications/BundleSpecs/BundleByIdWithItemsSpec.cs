using System;
using LaundryDomain.Models.BundleModules;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleByIdWithItemsSpec : BaseSpecification<Bundle, Guid>
    {
        public BundleByIdWithItemsSpec(Guid bundleId) : base(b => b.Id == bundleId)
        {
            AddInclude(b => b.BundleItems);
            AddInclude("BundleItems.Service");
            AddInclude(b => b.ApprovalRequests);
        }
    }
}
