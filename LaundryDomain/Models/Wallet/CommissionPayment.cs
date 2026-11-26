using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.WalletEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.Wallet
{
    public class CommissionPayment : BaseEntity<CommissionPayment, int>
    {
        [Required]
        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; } = null!;

        [Required]
        public string LaundryAdminId { get; set; } = string.Empty;
        public ApplicationUser LaundryAdmin { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "EGP";

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Kashier";

        [Required]
        [MaxLength(100)]
        public string MerchantOrderId { get; set; } = string.Empty;

        public CommissionPaymentStatus Status { get; set; } = CommissionPaymentStatus.Pending;

        public DateTime? PaidAt { get; set; }
    }
}
