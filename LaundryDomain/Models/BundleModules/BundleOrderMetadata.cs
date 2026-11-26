using LaundryDomain.Models.Common;
using LaundryDomain.Models.OrderModules;
using System;

namespace LaundryDomain.Models.BundleModules
{
    public class BundleOrderMetadata : BaseEntity<BundleOrderMetadata, Guid>
    {
        // OrderId is int because Order.Id is int
        public int OrderId { get; set; }
        public Guid BundleId { get; set; }
        public required string BundleNameSnapshot { get; set; }
        public decimal BundlePriceSnapshot { get; set; }
        public decimal OriginalPriceSnapshot { get; set; }
        public decimal SavingsAmount { get; set; }
        public DateTime PurchasedAt { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Bundle Bundle { get; set; } = null!;
    }
}
