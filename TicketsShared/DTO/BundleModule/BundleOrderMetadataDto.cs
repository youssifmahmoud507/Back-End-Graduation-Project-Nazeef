using System;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleOrderMetadataDto
    {
        public Guid BundleId { get; set; }
        public required string BundleName { get; set; }
        public decimal BundlePrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SavingsAmount { get; set; }
        public DateTime PurchasedAt { get; set; }
    }
}
