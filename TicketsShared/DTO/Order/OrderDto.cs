using LaundryShared.DTO.BundleModule;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal DiscountAmount { get; set; }
        public int LaundryId { get; set; }
        public string LaundryName { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public PaymentDto Payment { get; set; }

        // Bundle support
        public BundleOrderMetadataDto? BundleOrderMetadata { get; set; }
    }
}

