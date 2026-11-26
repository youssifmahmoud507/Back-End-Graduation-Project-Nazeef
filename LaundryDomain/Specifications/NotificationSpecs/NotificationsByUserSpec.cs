using LaundryDomain.Models.SystemModules;
using LaundryShared.DTO.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.NotificationSpecs
{
    public class NotificationsByUserSpec : BaseSpecification<Notification, int>
    {
        public NotificationsByUserSpec(string userId, NotificationParams notificationParams)
            : base(n => n.UserId == userId)
        {
            // Filter by Type
            if (notificationParams.Type.HasValue)
                AddCriteria(n => n.Type == notificationParams.Type.Value);

            // Filter by IsRead
            if (notificationParams.IsRead.HasValue)
                AddCriteria(n => n.IsRead == notificationParams.IsRead.Value);

            AddOrderByDescending(n => n.CreatedAt);

            ApplyPaging(
                (notificationParams.PageIndex - 1) * notificationParams.PageSize,
                notificationParams.PageSize
            );
        }
    }
}
