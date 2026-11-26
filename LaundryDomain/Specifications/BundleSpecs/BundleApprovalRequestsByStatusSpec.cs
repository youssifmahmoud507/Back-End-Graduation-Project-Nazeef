using System;
using LaundryDomain.Models.BundleModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleApprovalRequestsByStatusSpec : BaseSpecification<BundleApprovalRequest, Guid>
    {
        public BundleApprovalRequestsByStatusSpec(BundleApprovalStatus status, int pageIndex, int pageSize) 
            : base(r => r.Status == status)
        {
            AddInclude(r => r.Bundle);
            AddInclude("Bundle.BundleItems");
            AddInclude(r => r.RequestedByAdmin);
            AddOrderByDescending(r => r.RequestedAt);
            ApplyPaging(pageSize * (pageIndex - 1), pageSize);
        }
    }
}
