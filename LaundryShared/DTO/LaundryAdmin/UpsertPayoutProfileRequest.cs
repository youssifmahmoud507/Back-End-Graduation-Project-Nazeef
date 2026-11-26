using LaundryShared.Enums.WalletEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class UpsertPayoutProfileRequest
    {
        [Required]
        public PayoutTransferMethod TransferMethod { get; set; }

        public PayoutTransferType? TransferType { get; set; }

        [Required]
        [MaxLength(150)]
        public string RecipientFullName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? RecipientMobileNumber { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(34)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(25)]
        public string? CardNumber { get; set; }

        [MaxLength(20)]
        public string? NationalId { get; set; }
    }
}
