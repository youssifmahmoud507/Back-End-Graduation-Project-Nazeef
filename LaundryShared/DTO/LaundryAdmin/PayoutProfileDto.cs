using LaundryShared.Enums.WalletEnums;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class PayoutProfileDto
    {
        public int Id { get; set; }
        public int LaundryId { get; set; }
        public PayoutTransferMethod TransferMethod { get; set; }
        public PayoutTransferType? TransferType { get; set; }
        public string RecipientFullName { get; set; } = string.Empty;
        public string? RecipientMobileNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? CardNumber { get; set; }
        public string? NationalId { get; set; }
    }
}
