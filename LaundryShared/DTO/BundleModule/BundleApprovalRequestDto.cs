using System;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.BundleModule
{
    public class BundleApprovalRequestDto
    {
        public Guid Id { get; set; }
        public Guid BundleId { get; set; }
        public required string BundleName { get; set; }
        public int? LaundryId { get; set; }
        public string? LaundryName { get; set; }
        public required string RequestedByAdminId { get; set; }
        public required string RequestedByAdminName { get; set; }
        public BundleApprovalStatus Status { get; set; }
        public string? RequestNote { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsEditRequest { get; set; }
        public string? PendingBundleSnapshot { get; set; }
    }
}
