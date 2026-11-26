using LaundryShared.Enums.CustomerEnums;

namespace LaundryShared.DTO.User
{
    public class UserSettingsDto
    {
        public Language Language { get; set; }
        public Currency Currency { get; set; }
        public bool PushNotifications { get; set; }
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool WhatsappNotifications { get; set; }
        public bool ShowProfile { get; set; }
        public bool ShareData { get; set; }
    }
}
