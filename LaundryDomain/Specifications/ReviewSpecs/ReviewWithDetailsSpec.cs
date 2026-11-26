using LaundryDomain.Models.SystemModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Specifications.ReviewSpecs
{
    public class ReviewWithDetailsSpec : BaseSpecification<Review, int>
    {
        public ReviewWithDetailsSpec(int reviewId, string customerId)
            : base(r => r.Id == reviewId && r.CustomerId == customerId)
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Laundry);
        }
    }
}
