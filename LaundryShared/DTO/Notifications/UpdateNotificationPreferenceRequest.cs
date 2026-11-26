namespace LaundryShared.DTO.Notifications
{
    public class UpdateNotificationPreferenceRequest
    {
        public bool WhatsappEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool EmailEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public bool OrderUpdates { get; set; }
        public bool Promotions { get; set; }
        public bool Newsletter { get; set; }
    }
}
