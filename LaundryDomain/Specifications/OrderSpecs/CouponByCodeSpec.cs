using LaundryDomain.Models.OrderModules;
using LaundryDomain.Specifications;
using System;
using System.Linq.Expressions;

namespace LaundryDomain.Specifications.OrderSpecs
{
    public class CouponByCodeSpec : BaseSpecification<Coupon, int>
    {
        public CouponByCodeSpec(string code)
            : base(c => c.Code == code)
        {
        }
    }
}
