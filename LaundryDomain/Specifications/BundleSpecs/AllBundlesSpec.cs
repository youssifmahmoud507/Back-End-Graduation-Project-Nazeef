using System;
using System.Linq;
using LaundryDomain.Models.BundleModules;
using LaundryShared.DTO.BundleModule;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class AllBundlesSpec : BaseSpecification<Bundle, Guid>
    {
        public AllBundlesSpec(BundleSearchParams searchParams)
            : base(b =>
                (string.IsNullOrEmpty(searchParams.Keyword) || b.Name.Contains(searchParams.Keyword) || (b.Description != null && b.Description.Contains(searchParams.Keyword))) &&
                (!searchParams.Status.HasValue || b.Status == searchParams.Status) &&
                (!searchParams.MinPrice.HasValue || b.BundlePrice >= searchParams.MinPrice) &&
                (!searchParams.MaxPrice.HasValue || b.BundlePrice <= searchParams.MaxPrice) &&
                (!searchParams.LaundryId.HasValue || b.LaundryId == searchParams.LaundryId) &&
                (!searchParams.OwnerType.HasValue || b.OwnerType == searchParams.OwnerType)
            )
        {
            AddInclude(b => b.BundleItems);
            AddOrderByDescending(b => b.CreatedAt);
            ApplyPaging(searchParams.PageSize * (searchParams.PageIndex - 1), searchParams.PageSize);
        }
    }
}
