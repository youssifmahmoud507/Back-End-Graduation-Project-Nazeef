using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.OrderModules
{
    public class Order : BaseEntity<Order, int>
    {
        [Required]
        [MaxLength(250)]
        public string PickupLocation { get; set; }

        [Required]
        [MaxLength(250)]
        public string DeliveryLocation { get; set; }

        [Required]
        public DateTime PickupTime { get; set; }

        public DateTime? DeliveryTime { get; set; }

        [Required]
        [MaxLength(30)]
        public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public double? CourierLatitude { get; set; }
        public double? CourierLongitude { get; set; }

        // FKs
        [Required]
        public string CustomerId { get; set; }

        public string? CourierId { get; set; }

        [Required]
        public int LaundryId { get; set; }

        public int? CouponId { get; set; }

        public bool IsBundleOrder { get; set; } = false;
        public Guid? BundleOrderMetadataId { get; set; }

        // Navigation Properties
        public ApplicationUser Customer { get; set; }
        public ApplicationUser? Courier { get; set; }
        public Laundry Laundry { get; set; }
        public Coupon? Coupon { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment Payment { get; set; }

    }
}
