using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserFavoriteByLaundrySpec : BaseSpecification<UserFavorite, int>
    {
        public UserFavoriteByLaundrySpec(string userId, int laundryId)
            : base(f => f.UserId == userId && f.LaundryId == laundryId)
        {
        }
    }
}
