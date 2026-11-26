using System;
using System.Collections.Generic;

namespace LaundryShared.DTO.BundleModule
{
    public class PlaceBundleOrderRequest
    {
        public required Guid BundleId { get; init; }
        public required int LaundryId { get; init; }

        public string? PickupLocation { get; init; }
        public string? DeliveryLocation { get; init; }
        public required List<BundleItemSelectionDto> SelectedItems { get; init; }
        public bool UseWalletBalance { get; init; }
        public int? RedeemPoints { get; init; }
        public DateTime? ScheduledPickupTime { get; init; }
        public DateTime? ScheduledDeliveryTime { get; init; }
    }
}
