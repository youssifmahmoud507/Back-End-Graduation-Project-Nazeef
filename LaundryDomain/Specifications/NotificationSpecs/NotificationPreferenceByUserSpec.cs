using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.NotificationSpecs
{
    public class NotificationPreferenceByUserSpec : BaseSpecification<NotificationPreference, int>
    {
        public NotificationPreferenceByUserSpec(string userId)
            : base(n => n.UserId == userId)
        {
        }
    }
}
