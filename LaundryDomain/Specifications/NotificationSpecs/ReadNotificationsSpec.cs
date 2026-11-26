using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.NotificationSpecs
{
    public class ReadNotificationsSpec : BaseSpecification<Notification, int>
    {
        public ReadNotificationsSpec(string userId)
            : base(n => n.UserId == userId && n.IsRead)
        {
        }
    }
}
