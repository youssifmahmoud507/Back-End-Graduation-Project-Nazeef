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
    public class LaundryWithServicesSpec : BaseSpecification<Laundry, int>
    {
        // جلب مغسلة معينة بخدماتها
        public LaundryWithServicesSpec(int id)
            : base(l => l.Id == id && (l.Admin.AdminApprovalStatus == LaundryAdminApprovalStatus.Approved || l.Admin.Role == UserRole.SuperAdmin))
        {
            AddInclude(l => l.Services);
            AddInclude(l => l.Admin);
        }

        // جلب كل المغاسل مع الفلاتر والـ Pagination
        public LaundryWithServicesSpec(LaundryParams laundryParams)
        {
            AddInclude(l => l.Services);
            
            AddCriteria(l => l.Admin.AdminApprovalStatus == LaundryAdminApprovalStatus.Approved || l.Admin.Role == UserRole.SuperAdmin);

            // Search by Name
            if (!string.IsNullOrEmpty(laundryParams.Search))
                AddCriteria(l => l.Name.ToLower().Contains(laundryParams.Search.ToLower()));

            // Filter by Availability
            if (!string.IsNullOrEmpty(laundryParams.Availability))
                AddCriteria(l => l.Availability.ToString() == laundryParams.Availability);

            // Filter by MinRating
            if (laundryParams.MinRating.HasValue)
                AddCriteria(l => l.AverageRating >= laundryParams.MinRating.Value);

            // Sorting
            if (laundryParams.SortBy == "rating")
                AddOrderByDescending(l => l.AverageRating);
            else
                AddOrderBy(l => l.Name);

            // Pagination
            ApplyPaging(
                (laundryParams.PageIndex - 1) * laundryParams.PageSize,
                laundryParams.PageSize
            );
        }
    }
}
