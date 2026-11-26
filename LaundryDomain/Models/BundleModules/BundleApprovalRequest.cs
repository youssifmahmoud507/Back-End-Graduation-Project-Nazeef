using System;
using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;


namespace LaundryDomain.Models.BundleModules
{
    public class BundleApprovalRequest : BaseEntity<BundleApprovalRequest, Guid>
    {
        public Guid BundleId { get; set; }
        public required string RequestedByAdminId { get; set; }
        public string? ReviewedByAdminId { get; set; }
        public BundleApprovalStatus Status { get; set; }
        public string? RequestNote { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsEditRequest { get; set; }
        public string? PendingBundleSnapshot { get; set; }

        public Bundle Bundle { get; set; } = null!;
        public ApplicationUser RequestedByAdmin { get; set; } = null!;
        public ApplicationUser? ReviewedByAdmin { get; set; }
    }
}
