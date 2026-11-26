using System;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleOrderMetadataByOrderSpec
        : BaseSpecification<BundleOrderMetadata, Guid>
    {
        // orderId is int because Order.Id is int
        public BundleOrderMetadataByOrderSpec(int orderId)
            : base(m => m.OrderId == orderId)
        {
            AddInclude(m => m.Bundle);
        }
    }
}