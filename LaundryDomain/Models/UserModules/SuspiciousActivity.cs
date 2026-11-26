using LaundryDomain.Models.Common;
using LaundryShared.Enums.Fraud;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaundryDomain.Models.UserModules
{
    public class SuspiciousActivity : BaseEntity<SuspiciousActivity, int>
    {

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string? ActionType { get; set; }
        public string? Reason { get; set; }
        public int RiskScore { get; set; }
        public string? IpAddress { get; set; }
        public string? AiRecommendedAction { get; set; } // Allow, Warn, Block

        public SuspiciousActivityStatus Status { get; set; } = SuspiciousActivityStatus.Open;

        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
