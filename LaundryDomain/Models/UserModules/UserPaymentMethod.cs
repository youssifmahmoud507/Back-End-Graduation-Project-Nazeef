using LaundryDomain.Models.Common;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Models.UserModules
{
    public class UserPaymentMethod : BaseEntity<UserPaymentMethod, int>
    {
        public PaymentMethodType Type { get; set; } // credit_card, debit_card, wallet
        public string CardLast4 { get; set; }
        public CardBrand CardBrand { get; set; } // visa, mastercard
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CardholderName { get; set; }
        public bool IsDefault { get; set; } = false;

        // FK
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
