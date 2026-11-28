using LaundryServiesAbstraction.IServices.Fraud;
using LaundryShared.DTO.Fraud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Betalgo.Ranul.OpenAI.ObjectModels.SharedModels.IOpenAIModels;

namespace LaundryApi.Controllers
{

        /// <summary>
        /// البوابة بتاعة الـ Fraud Detection
        /// بيكشف العمليات المشبوهة
        /// </summary>
        [Route("api/fraud")]
        [ApiController]
        public class FraudController : ControllerBase
        {
            private readonly IFraudService _fraudService;

            public FraudController(IFraudService fraudService)
            {
                _fraudService = fraudService;
            }

            private string GetUserId() => User.FindFirstValue("uid");

            /// <summary>
            /// POST /api/fraud/analyze
            /// بيحلل عملية معينة
            /// </summary>
            [HttpPost("analyze")]
            [Authorize]
        public async Task<IActionResult> AnalyzeAction(
                [FromBody] FraudDetectionDto request)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _fraudService.AnalyzeActionAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(result.Value);
            }

            /// <summary>
            /// GET /api/fraud/behavior
            /// بيحلل سلوك اليوزر الحالي
            /// </summary>
            [HttpGet("behavior")]
            [Authorize]
            public async Task<IActionResult> AnalyzeBehavior()
            {
                var result = await _fraudService
                    .AnalyzeUserBehaviorAsync(GetUserId());

                if (!result.IsSuccess)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(result.Value);
            }

            /// <summary>
            /// POST /api/fraud/payment
            /// بيحلل عملية دفع مشبوهة
            /// SuperAdmin بس
            /// </summary>
            [HttpPost("payment")]
            [Authorize(Roles = "SuperAdmin")]
            public async Task<IActionResult> AnalyzePayment(
                [FromBody] AnalyzePaymentRequest request)
            {
                var result = await _fraudService.AnalyzePaymentAsync(
                    request.UserId,
                    request.Amount,
                    request.IpAddress);

                if (!result.IsSuccess)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(result.Value);
            }

            /// <summary>
            /// GET /api/fraud/alerts
            /// بيجيب كل التنبيهات المعلقة
            /// </summary>
            [HttpGet("alerts")]
            [Authorize(Roles = "SuperAdmin")]
            public async Task<IActionResult> GetPendingAlerts()
            {
                var result = await _fraudService.GetPendingAlertsAsync();
                
                if (!result.IsSuccess)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(result.Value);
            }

            public class ReviewAlertRequest
            {
                public bool ApproveBan { get; set; }
            }

            /// <summary>
            /// PUT /api/fraud/alerts/{id}/review
            /// بيراجع تنبيه معين
            /// </summary>
            [HttpPut("alerts/{id}/review")]
            [Authorize(Roles = "SuperAdmin")]
            public async Task<IActionResult> ReviewAlert(int id, [FromBody] ReviewAlertRequest request)
            {
                var result = await _fraudService.ReviewAlertAsync(id, request.ApproveBan);

                if (!result.IsSuccess)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(new { Message = "Alert reviewed successfully" });
            }
        }
    }
