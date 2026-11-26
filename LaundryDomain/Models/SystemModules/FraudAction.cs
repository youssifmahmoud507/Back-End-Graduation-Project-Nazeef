using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.SystemModules
{
    public enum FraudAlertStatus
    {
        Pending,
        ApprovedBan,
        Dismissed
    }

    public class FraudAction : BaseEntity<FraudAction, int>
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public float RiskScore { get; set; }

        public FraudAlertStatus Status { get; set; } = FraudAlertStatus.Pending;

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public string? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // Navigation
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser? Admin { get; set; }
    }
}
