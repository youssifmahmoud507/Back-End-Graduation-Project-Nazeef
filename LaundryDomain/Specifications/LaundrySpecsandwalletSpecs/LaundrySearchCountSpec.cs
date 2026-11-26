using LaundryDomain.Models.LaundryModules;
using LaundryShared.DTO.Laundry;
using LaundryShared.Enums;

namespace LaundryDomain.Specifications.LaundrySpecs
{
    public class LaundrySearchCountSpec : BaseSpecification<Laundry, int>
    {
        public LaundrySearchCountSpec(LaundrySearchParams searchParams)
        {
            AddCriteria(l => l.Admin.AdminApprovalStatus == LaundryAdminApprovalStatus.Approved || l.Admin.Role == UserRole.SuperAdmin);

            // Search by Name
            if (!string.IsNullOrEmpty(searchParams.Q))
                AddCriteria(l => l.Name.ToLower().Contains(searchParams.Q.ToLower()));

            // Filter by Location (Radius)
            if (searchParams.Lat.HasValue && searchParams.Lng.HasValue && searchParams.Radius.HasValue)
                AddCriteria(l =>
                    Math.Sqrt(
                        Math.Pow((double)(l.Latitude - searchParams.Lat.Value) * 111.0, 2) +
                        Math.Pow((double)(l.Longitude - searchParams.Lng.Value) * 111.0, 2)
                    ) <= searchParams.Radius.Value
                );
        }
    }
}
