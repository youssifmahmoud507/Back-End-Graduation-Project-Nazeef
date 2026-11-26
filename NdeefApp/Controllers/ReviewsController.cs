using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.Reviews;
using LaundryShared.DTO.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;


        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private string GetCustomerId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // POST /api/reviews
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewService.AddReviewAsync(GetCustomerId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/reviews/order/:orderId
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> CheckOrderReview(int orderId)
        {
            var result = await _reviewService.CheckOrderReviewAsync(GetCustomerId(), orderId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/reviews/:reviewId
        [HttpPut("{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewService.UpdateReviewAsync(GetCustomerId(), reviewId, request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // DELETE /api/reviews/:reviewId
        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var result = await _reviewService.DeleteReviewAsync(GetCustomerId(), reviewId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Review deleted successfully." });
        }

        // GET /api/reviews/my-reviews
        [HttpGet("my-reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            var result = await _reviewService.GetMyReviewsAsync(GetCustomerId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }



    }
}
