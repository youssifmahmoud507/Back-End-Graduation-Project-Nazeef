using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.NotificationSpecs
{
    public class UnreadNotificationsSpec : BaseSpecification<Notification, int>
    {
        public UnreadNotificationsSpec(string userId)
            : base(n => n.UserId == userId && !n.IsRead)
        {
            AddOrderByDescending(n => n.CreatedAt);
        }
    }
}
