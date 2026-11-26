using System;
using System.Linq;
using LaundryDomain.Models.BundleModules;
using LaundryShared.DTO.BundleModule;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class AllBundlesCountSpec : BaseSpecification<Bundle, Guid>
    {
        public AllBundlesCountSpec(BundleSearchParams searchParams)
            : base(b =>
                (string.IsNullOrEmpty(searchParams.Keyword) || b.Name.Contains(searchParams.Keyword) || (b.Description != null && b.Description.Contains(searchParams.Keyword))) &&
                (!searchParams.Status.HasValue || b.Status == searchParams.Status) &&
                (!searchParams.MinPrice.HasValue || b.BundlePrice >= searchParams.MinPrice) &&
                (!searchParams.MaxPrice.HasValue || b.BundlePrice <= searchParams.MaxPrice) &&
                (!searchParams.LaundryId.HasValue || b.LaundryId == searchParams.LaundryId) &&
                (!searchParams.OwnerType.HasValue || b.OwnerType == searchParams.OwnerType)
            )
        {
        }
    }
}
