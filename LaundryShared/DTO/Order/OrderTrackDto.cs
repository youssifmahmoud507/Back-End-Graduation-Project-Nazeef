using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Order
{
    public class OrderTrackDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string LaundryName { get; set; }
        public string LaundryAddress { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public decimal? CourierLatitude { get; set; }
        public decimal? CourierLongitude { get; set; }
        public string? CourierId { get; set; }
        public string? CourierName { get; set; }
        public string? CourierPhone { get; set; }
        public string? CourierAvatar { get; set; }
    }
}

