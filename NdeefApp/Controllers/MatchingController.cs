using LaundryServiesAbstraction.IServices.Matching;
using LaundryShared.DTO.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Betalgo.Ranul.OpenAI.ObjectModels.SharedModels.IOpenAIModels;

namespace LaundryApi.Controllers
{
    /// <summary>
    /// البوابة بتاعة الـ Smart Matching
    /// بيختار أحسن مغسلة للعميل بالـ AI
    /// </summary>
    [Route("api/matching")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;

        public MatchingController(IMatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        private string GetCustomerId() => User.FindFirstValue("uid");

        /// <summary>
        /// POST /api/matching/best
        /// بيرجع أحسن مغسلة واحدة للعميل
        /// </summary>
        [HttpPost("best")]
        public async Task<IActionResult> GetBestMatch(
            [FromBody] MatchingRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _matchingService.GetBestMatchAsync(
                GetCustomerId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// POST /api/matching/top
        /// بيرجع أحسن 3 مغاسل للعميل عشان يختار
        /// </summary>
        [HttpPost("top")]
        public async Task<IActionResult> GetTopMatches(
            [FromBody] MatchingRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _matchingService.GetTopMatchesAsync(
                GetCustomerId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }
    }
}
