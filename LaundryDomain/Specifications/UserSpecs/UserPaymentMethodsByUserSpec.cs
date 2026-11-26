using LaundryDomain.Models.UserModules;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserPaymentMethodsByUserSpec : BaseSpecification<UserPaymentMethod, int>
    {
        public UserPaymentMethodsByUserSpec(string userId)
            : base(p => p.UserId == userId)
        {
            AddOrderByDescending(p => p.IsDefault);
        }
    }
}
