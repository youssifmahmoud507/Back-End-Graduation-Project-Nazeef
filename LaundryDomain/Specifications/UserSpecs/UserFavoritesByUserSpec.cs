using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserFavoritesByUserSpec : BaseSpecification<UserFavorite, int>
    {
        public UserFavoritesByUserSpec(string userId)
            : base(f => f.UserId == userId)
        {
            AddInclude(f => f.Laundry);
            AddOrderByDescending(f => f.CreatedAt);
        }
    }
}
