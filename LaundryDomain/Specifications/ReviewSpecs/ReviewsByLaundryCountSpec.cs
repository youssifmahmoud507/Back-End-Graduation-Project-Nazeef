using LaundryDomain.Models.SystemModules;

namespace LaundryDomain.Specifications.ReviewSpecs
{
    public class ReviewsByLaundryCountSpec : BaseSpecification<Review, int>
    {
        public ReviewsByLaundryCountSpec(int laundryId, int? rating = null)
            : base(r => r.LaundryId == laundryId)
        {
            if (rating.HasValue)
                AddCriteria(r => r.Rating == rating.Value);
        }
    }
}
