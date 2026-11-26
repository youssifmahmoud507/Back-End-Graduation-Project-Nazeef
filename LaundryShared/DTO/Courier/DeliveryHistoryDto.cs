using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Courier
{
    public class DeliveryHistoryDto
    {
    // Delivery History item
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal EarningsFromOrder { get; set; }
        public OrderStatus FinalStatus { get; set; }
    }
}
