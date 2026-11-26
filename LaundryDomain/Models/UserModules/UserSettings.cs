using LaundryDomain.Models.Common;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Models.UserModules
{
    public class UserSettings : BaseEntity<UserSettings, int>
    {
        public Language Language { get; set; } = Language.English;
        public Currency Currency { get; set; } = Currency.USD;
        public bool PushNotifications { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = true;
        public bool WhatsappNotifications { get; set; } = true;
        public bool ShowProfile { get; set; } = true;
        public bool ShareData { get; set; } = false;

        // FK - One to One
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
