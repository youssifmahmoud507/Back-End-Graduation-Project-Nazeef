using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Order
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// المبلغ الذي تم خصمه تلقائياً من محفظة العميل
        /// </summary>
        public decimal WalletDeduction { get; set; }

        /// <summary>
        /// المبلغ المتبقي على العميل دفعه (بعد خصم المحفظة)
        /// </summary>
        public decimal AmountDue { get; set; }
    }
}

