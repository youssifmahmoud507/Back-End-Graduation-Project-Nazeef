using System;
using LaundryDomain.Models.BundleModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleApprovalRequestsByStatusCountSpec : BaseSpecification<BundleApprovalRequest, Guid>
    {
        public BundleApprovalRequestsByStatusCountSpec(BundleApprovalStatus status) 
            : base(r => r.Status == status)
        {
        }
    }
}
