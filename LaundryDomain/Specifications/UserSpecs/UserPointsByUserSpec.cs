using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserPointsByUserSpec : BaseSpecification<UserPoints, int>
    {
        public UserPointsByUserSpec(string userId)
            : base(p => p.UserId == userId)
        {
            AddInclude(p => p.Transactions);
        }
    }
}
