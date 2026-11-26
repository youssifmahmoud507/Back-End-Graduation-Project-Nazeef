using System;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleItemSelectionDto
    {
        public required Guid BundleItemId { get; init; }
        public required int ServiceId { get; init; }
        public required int Quantity { get; init; }
    }
}
