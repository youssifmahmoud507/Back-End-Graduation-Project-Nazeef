using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.User
{
    public class UserPaymentMethodDto
    {
        public int Id { get; set; }
        public PaymentMethodType Type { get; set; }
        public string CardLast4 { get; set; }
        public CardBrand CardBrand { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CardholderName { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
