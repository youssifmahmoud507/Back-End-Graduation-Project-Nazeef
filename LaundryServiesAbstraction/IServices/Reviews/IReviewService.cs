using LaundryShared.Common;
using LaundryShared.DTO.Laundry;
using LaundryShared.DTO.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Reviews
{
    public interface IReviewService
    {
        // POST /api/reviews
        Task<Result<ReviewDto>> AddReviewAsync(string customerId, AddReviewRequest request);

        // GET /api/reviews/order/:orderId
        Task<Result<OrderReviewCheckDto>> CheckOrderReviewAsync(string customerId, int orderId);

        // PUT /api/reviews/:reviewId
        Task<Result<ReviewDto>> UpdateReviewAsync(string customerId, int reviewId, UpdateReviewRequest request);

        // DELETE /api/reviews/:reviewId
        Task<Result<bool>> DeleteReviewAsync(string customerId, int reviewId);

        // GET /api/reviews/my-reviews
        Task<Result<List<ReviewDto>>> GetMyReviewsAsync(string customerId);

        // GET /api/laundries/:laundryId/reviews
        Task<Result<Pagination<ReviewDto>>> GetLaundryReviewsAsync(int laundryId, int pageIndex, int pageSize, int? rating = null);

        // GET /api/laundries/:laundryId/rating-summary
        Task<Result<RatingSummaryDto>> GetRatingSummaryAsync(int laundryId);
    }
}
