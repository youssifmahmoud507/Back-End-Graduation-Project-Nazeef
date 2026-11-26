using LaundryDomain.Models.LaundryModules;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.ServiceSpecs
{
    public class ServicesByCategorySpec : BaseSpecification<Service, int>
    {
        public ServicesByCategorySpec(ServiceCategory category)
            : base(s => s.Category == category && s.IsAvailable)
        {
            AddInclude(s => s.Laundry);
            AddOrderBy(s => s.Price);
        }
    }
}
