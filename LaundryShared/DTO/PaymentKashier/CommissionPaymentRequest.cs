using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.PaymentKashier
{
    public class CommissionPaymentRequest
    {
        [Required]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "EGP";

        [Required]
        public int LaundryId { get; set; }
    }
}
