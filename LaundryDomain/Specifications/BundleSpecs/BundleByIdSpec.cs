using System;
using LaundryDomain.Models.BundleModules;
using LaundryDomain.Specifications;

namespace LaundryDomain.Specifications.BundleSpecs
{
    public class BundleByIdSpec : BaseSpecification<Bundle, Guid>
    {
        public BundleByIdSpec(Guid id) : base(b => b.Id == id)
        {
            AddInclude(b => b.BundleItems);
        }
    }
}