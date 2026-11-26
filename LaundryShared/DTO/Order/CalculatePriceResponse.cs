namespace LaundryShared.DTO.Order
{
    public class CalculatePriceResponse
    {
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public List<OrderItemPriceDto> Items { get; set; } = new List<OrderItemPriceDto>();
    }
}

