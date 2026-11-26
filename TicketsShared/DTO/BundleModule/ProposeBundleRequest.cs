using System;
using System.Collections.Generic;

namespace LaundryShared.DTO.BundleModule
{
    public class ProposeBundleRequest
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required decimal BundlePrice { get; init; }
        public required decimal OriginalPrice { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? ExpiryDate { get; init; }
        public string? ImageUrl { get; init; }
        public int DisplayOrder { get; init; }

        public required List<CreateBundleItemRequest> BundleItems { get; init; }
        public string? RequestNote { get; init; }
    }
}
