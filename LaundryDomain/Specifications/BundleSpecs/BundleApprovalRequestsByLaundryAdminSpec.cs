using System;
using LaundryDomain.Models.BundleModules;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleApprovalRequestsByLaundryAdminSpec : BaseSpecification<BundleApprovalRequest, Guid>
    {
        public BundleApprovalRequestsByLaundryAdminSpec(string laundryAdminId) 
            : base(r => r.RequestedByAdminId == laundryAdminId)
        {
            AddInclude(r => r.Bundle);
            AddOrderByDescending(r => r.RequestedAt);
        }
    }
}
