using System;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleItemDto
    {
        public Guid Id { get; set; }
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public ServiceCategory ServiceCategory { get; set; }
        public int Quantity { get; set; }
        public ServiceCategory? AllowedItemCategory { get; set; }
    }
}
