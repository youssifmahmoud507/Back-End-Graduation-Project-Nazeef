using System;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class ReviewBundleApprovalRequest
    {
        public required Guid ApprovalRequestId { get; init; }
        public required BundleApprovalStatus Decision { get; init; }
        public string? ReviewNote { get; init; }
    }
}
