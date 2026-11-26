using System;
using System.Collections.Generic;

namespace LaundryShared.DTO.PaymentKashier
{
    public class CustomerWalletDto
    {
        public decimal Balance { get; set; }
        public decimal TotalCharged { get; set; }
        public bool IsActive { get; set; }
        public List<WalletTransactionDto> Transactions { get; set; } = new List<WalletTransactionDto>();
    }

    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentReference { get; set; }
        public string Type { get; set; } = string.Empty; // Credit / Debit
        public string Source { get; set; } = string.Empty; // WalletCharge / WalletPayment / Refund etc.
        public string Status { get; set; } = string.Empty; // Pending / Completed / Failed
        public DateTime CreatedAt { get; set; }
    }
}
