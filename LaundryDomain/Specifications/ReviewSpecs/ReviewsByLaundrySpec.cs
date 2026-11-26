using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.ReviewSpecs
{
    public class ReviewsByLaundrySpec : BaseSpecification<Review, int>
    {
        public ReviewsByLaundrySpec(int laundryId, int pageIndex, int pageSize, int? rating = null)
            : base(r => r.LaundryId == laundryId)
        {
            AddInclude(r => r.Customer);

            if (rating.HasValue)
                AddCriteria(r => r.Rating == rating.Value);

            AddOrderByDescending(r => r.CreatedAt);

            ApplyPaging((pageIndex - 1) * pageSize, pageSize);
        }
    }
}
