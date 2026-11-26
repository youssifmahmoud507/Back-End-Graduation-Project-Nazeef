using LaundryDomain.Models.SystemModules;
using LaundryShared.DTO.Notifications;

namespace LaundryDomain.Specifications.NotificationSpecs
{
    public class NotificationsByUserCountSpec : BaseSpecification<Notification, int>
    {
        public NotificationsByUserCountSpec(string userId, NotificationParams notificationParams)
            : base(n => n.UserId == userId)
        {
            if (notificationParams.Type.HasValue)
                AddCriteria(n => n.Type == notificationParams.Type.Value);

            if (notificationParams.IsRead.HasValue)
                AddCriteria(n => n.IsRead == notificationParams.IsRead.Value);
        }
    }
}
