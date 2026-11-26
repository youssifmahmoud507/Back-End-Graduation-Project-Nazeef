using System;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class SetBundleStatusRequest
    {
        public required Guid BundleId { get; init; }
        public required BundleStatus NewStatus { get; init; }
    }
}
