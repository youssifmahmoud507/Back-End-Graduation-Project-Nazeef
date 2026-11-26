using LaundryDomain.Models.LaundryModules;
using LaundryShared.DTO.Laundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LaundryShared.Enums;

namespace LaundryDomain.Specifications.LaundrySpecs
{
    public class LaundryForCountSpec : BaseSpecification<Laundry, int>
    {
        public LaundryForCountSpec(LaundryParams laundryParams)
        {
            AddCriteria(l => l.Admin.AdminApprovalStatus == LaundryAdminApprovalStatus.Approved || l.Admin.Role == UserRole.SuperAdmin);
            if (!string.IsNullOrEmpty(laundryParams.Search))
                AddCriteria(l => l.Name.ToLower().Contains(laundryParams.Search.ToLower()));

            if (!string.IsNullOrEmpty(laundryParams.Availability))
                AddCriteria(l => l.Availability.ToString() == laundryParams.Availability);

            if (laundryParams.MinRating.HasValue)
                AddCriteria(l => l.AverageRating >= laundryParams.MinRating.Value);
        }
    }
}
