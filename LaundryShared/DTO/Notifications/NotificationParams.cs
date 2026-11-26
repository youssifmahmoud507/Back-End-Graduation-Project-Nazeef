using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.Notifications
{
    public class NotificationParams
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 20;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public NotificationType? Type { get; set; }
        public bool? IsRead { get; set; }
    }
}
