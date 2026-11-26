using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Order
{
    public class OrderParams
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public OrderStatus? Status { get; set; }
    }
}

