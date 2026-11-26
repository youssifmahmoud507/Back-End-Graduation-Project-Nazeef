using LaundryDomain.Models.UserModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.UserSpecs
{
    public class UserAddressesByUserSpec : BaseSpecification<UserAddress, int>
    {
        public UserAddressesByUserSpec(string userId)
            : base(a => a.UserId == userId)
        {
            AddOrderByDescending(a => a.IsDefault);
        }
    }
}
