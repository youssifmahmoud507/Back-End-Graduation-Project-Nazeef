using System;
using System.Collections.Generic;

namespace LaundryShared.DTO.BundleModule
{
    public class UpdateBundleRequest
    {
        public required Guid BundleId { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? BundlePrice { get; init; }
        public decimal? OriginalPrice { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? ExpiryDate { get; init; }
        public List<CreateBundleItemRequest>? BundleItems { get; init; }
    }
}
