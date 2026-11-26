using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserSettingsByUserSpec : BaseSpecification<UserSettings, int>
    {
        public UserSettingsByUserSpec(string userId)
            : base(s => s.UserId == userId)
        {
        }
    }
}
