using System;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleApprovalRequestByIdSpec : BaseSpecification<BundleApprovalRequest, Guid>
    {
        public BundleApprovalRequestByIdSpec(Guid id) : base(r => r.Id == id)
        {
            AddInclude(r => r.Bundle);
            AddInclude("Bundle.BundleItems");
            AddInclude(r => r.RequestedByAdmin);
            AddInclude(r => r.ReviewedByAdmin);
        }
    }
}