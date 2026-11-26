using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Courier
{
    public class CourierOrderParams
    {
        // Filter params لـ GET /api/courier/orders
        public CourierOrderFilter Filter { get; set; } = CourierOrderFilter.All;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
