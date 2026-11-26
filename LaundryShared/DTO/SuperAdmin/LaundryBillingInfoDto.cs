using System;

namespace LaundryShared.DTO.SuperAdmin
{
    public class LaundryBillingInfoDto
    {
        public int LaundryId { get; set; }
        public string LaundryName { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        
        // Payout Profile details
        public string TransferMethod { get; set; } = string.Empty;
        public string? TransferType { get; set; }
        public string RecipientFullName { get; set; } = string.Empty;
        public string? RecipientMobileNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? CardNumber { get; set; }
        public string? NationalId { get; set; }

        // Wallet details
        public decimal TotalEarnings { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PayoutableAmount { get; set; }
        public decimal PendingCommission { get; set; }
        public decimal DebtCeiling { get; set; }
        public string WalletStatus { get; set; } = string.Empty;
        public DateTime? LastPayoutDate { get; set; }
    }
}
