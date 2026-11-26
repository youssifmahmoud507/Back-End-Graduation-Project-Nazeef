using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Order
{
    public class OrderItemRequest
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
