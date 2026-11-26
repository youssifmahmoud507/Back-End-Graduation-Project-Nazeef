using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Order
{
    public class CalculatePriceRequest
    {
        [Required]
        public int LaundryId { get; set; }

        [Required]
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();

        public string? CouponCode { get; set; }
    }
}

