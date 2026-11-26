using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.Order
{
    public class PlaceOrderRequest
    {
        [Required]
        public int LaundryId { get; set; }

        [Required]
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();

        [Required]
        public DateTime PickupTime { get; set; }

        public DateTime? DeliveryTime { get; set; }

        [Required]
        [MaxLength(250)]
        public string PickupLocation { get; set; }

        [Required]
        [MaxLength(250)]
        public string DeliveryLocation { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? CouponCode { get; set; }
    }
}
