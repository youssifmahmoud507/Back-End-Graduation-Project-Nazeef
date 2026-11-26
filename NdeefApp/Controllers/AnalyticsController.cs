using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{

    /// <summary>
    /// البوابة بتاعة التقارير والإحصائيات
    /// فيه Endpoint لصاحب المغسلة وواحد للـ Super Admin
    /// </summary>
    [Route("api/analytics")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IDemandForecastService _demandForecastService;

        public AnalyticsController(IAnalyticsService analyticsService,
                                    IDemandForecastService demandForecastService)
        {
            _analyticsService = analyticsService;
            _demandForecastService = demandForecastService;
        }

        // Helper بيجيب الـ AdminId من الـ JWT Token
        private string GetAdminId() => User.FindFirstValue("uid");

        /// <summary>
        /// GET /api/analytics/laundry
        /// تقرير صاحب المغسلة
        /// أرباحه وطلباته وأكتر خدمة مطلوبة
        /// </summary>
        [HttpGet("laundry")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> GetLaundryAnalytics()
        {
            var result = await _analyticsService.GetLaundryAnalyticsAsync(GetAdminId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// GET /api/analytics/system
        /// تقرير الـ Super Admin
        /// إحصائيات السيستم كله
        /// </summary>
        [HttpGet("system")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetSystemAnalytics()
        {
            var result = await _analyticsService.GetSystemAnalyticsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }
        [HttpGet("forecast")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> GetForecast()
        {
            var analytics = await _analyticsService.GetLaundryAnalyticsAsync(GetAdminId());
            if (!analytics.IsSuccess)
                return BadRequest(new { Message = analytics.ErrorMessage });

            var forecast = await _demandForecastService.GetDemandForecastAsync(analytics.Value);
            if (forecast == null)
                return BadRequest("Could not get demand forecast");

            return Ok(forecast);
        }
    }
}
