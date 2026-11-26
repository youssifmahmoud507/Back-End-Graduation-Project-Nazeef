using LaundryDomain.Models.Common;

namespace LaundryDomain.Models.UserModules
{
    public class NotificationPreference : BaseEntity<NotificationPreference, int>
    {
        public bool WhatsappEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public bool OrderUpdates { get; set; } = true;
        public bool Promotions { get; set; } = true;
        public bool Newsletter { get; set; } = false;

        // FK - One to One
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
