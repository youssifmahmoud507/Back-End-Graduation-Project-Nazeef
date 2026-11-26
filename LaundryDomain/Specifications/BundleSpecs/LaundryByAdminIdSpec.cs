using System;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class LaundryByAdminIdSpec : BaseSpecification<Laundry, int>
    {
        public LaundryByAdminIdSpec(string adminId)
            : base(l => l.AdminId == adminId)
        {
        }
    }
}