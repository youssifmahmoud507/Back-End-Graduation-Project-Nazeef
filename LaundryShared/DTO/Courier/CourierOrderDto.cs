using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.Courier
{
    public class CourierOrderDto
    {
        public int Id { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }

        // Customer info
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // Laundry info
        public string LaundryName { get; set; }
        public string LaundryAddress { get; set; }
        public decimal? LaundryLatitude { get; set; }
        public decimal? LaundryLongitude { get; set; }

        // Timestamps
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Courier current location
        public double? CourierLatitude { get; set; }
        public double? CourierLongitude { get; set; }

        // App-facing fields (used by courier dashboard)
        public bool IsPaid { get; set; }
        public int ItemsCount { get; set; }
        public string ServiceName { get; set; } = "Laundry Service";
    }
}
