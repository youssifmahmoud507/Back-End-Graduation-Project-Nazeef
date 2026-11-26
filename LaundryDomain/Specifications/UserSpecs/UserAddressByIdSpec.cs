using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserAddressByIdSpec : BaseSpecification<UserAddress, int>
    {
        public UserAddressByIdSpec(int addressId, string userId)
            : base(a => a.Id == addressId && a.UserId == userId)
        {
        }
    }
}
