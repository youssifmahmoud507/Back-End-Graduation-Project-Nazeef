using System;
using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Models.BundleModules
{
    public class BundleItem : BaseEntity<BundleItem, Guid>
    {
        public Guid BundleId { get; set; }
        public int ServiceId { get; set; } // Matches Service Entity Id typing
        public int Quantity { get; set; }
        public ServiceCategory? AllowedItemCategory { get; set; }

        public Bundle Bundle { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
