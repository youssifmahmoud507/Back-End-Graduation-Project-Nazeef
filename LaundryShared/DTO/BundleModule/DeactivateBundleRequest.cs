using System;

namespace LaundryShared.DTO.BundleModule
{
    public class DeactivateBundleRequest
    {
        public Guid? BundleId { get; init; }
        public string? Reason { get; init; }
    }
}
