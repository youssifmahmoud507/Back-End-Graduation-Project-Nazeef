using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.User
{
    public class AddPaymentMethodRequest
    {
        [Required]
        public PaymentMethodType Type { get; set; }

        [Required]
        [CreditCard]
        public string CardNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string CardholderName { get; set; }

        [Required]
        [MaxLength(2)]
        public string ExpiryMonth { get; set; }

        [Required]
        [MaxLength(4)]
        public string ExpiryYear { get; set; }

        [Required]
        [MaxLength(4)]
        public string Cvv { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
