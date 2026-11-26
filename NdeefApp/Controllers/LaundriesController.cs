using LaundryServiesAbstraction.IServices.Laundry;
using LaundryServiesAbstraction.IServices.Reviews;
using LaundryShared.DTO.Laundry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LaundriesController : ControllerBase
    {
        private readonly ILaundryService _laundryService;
        private readonly IReviewService _reviewService;

        public LaundriesController(ILaundryService laundryService, IReviewService reviewService)
        {
            _laundryService = laundryService;
            _reviewService = reviewService;
        }


        // GET /api/laundries
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllLaundries([FromQuery] LaundryParams laundryParams)
        {
            var result = await _laundryService.GetAllLaundriesAsync(laundryParams);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/laundries/search
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchLaundries([FromQuery] LaundrySearchParams searchParams)
        {
            var result = await _laundryService.SearchLaundriesAsync(searchParams);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/laundries/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLaundryById(int id)
        {
            var result = await _laundryService.GetLaundryByIdAsync(id);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/laundries/{id}/services
        [HttpGet("{id}/services")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLaundryServices(int id)
        {
            var result = await _laundryService.GetLaundryServicesAsync(id);

            if (!result.IsSuccess)
                return NotFound(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/laundries/:laundryId/reviews
        [HttpGet("{laundryId}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLaundryReviews(
            int laundryId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? rating = null)
        {
            var result = await _reviewService.GetLaundryReviewsAsync(laundryId, pageIndex, pageSize, rating);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/laundries/:laundryId/rating-summary
        [HttpGet("{laundryId}/rating-summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRatingSummary(int laundryId)
        {
            var result = await _reviewService.GetRatingSummaryAsync(laundryId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }


    }
}

