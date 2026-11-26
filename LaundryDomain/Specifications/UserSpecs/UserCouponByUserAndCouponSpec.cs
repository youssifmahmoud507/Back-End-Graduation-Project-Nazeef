using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserCouponByUserAndCouponSpec : BaseSpecification<UserCoupon, int>
    {
        public UserCouponByUserAndCouponSpec(string userId, int couponId)
            : base(uc => uc.UserId == userId && uc.CouponId == couponId)
        {
        }
    }
}
