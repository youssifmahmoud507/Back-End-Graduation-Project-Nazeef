using LaundryServiesAbstraction.IServices.Courier;
using LaundryShared.DTO.Courier;
using LaundryShared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourierController : ControllerBase
    {
        private readonly ICourierService _courierService;

        public CourierController(ICourierService courierService)
        {
            _courierService = courierService;
        }

        private string GetCourierId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        private bool IsCourier()
        {
            return User.IsInRole(UserRole.Courier.ToString());
        }

        // ──────────────────────────────────────────────
        // 1. GET /api/courier/orders
        // ──────────────────────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetAssignedOrders([FromQuery] CourierOrderParams orderParams)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.GetAssignedOrdersAsync(GetCourierId(), orderParams);
            return Ok(result);
        }

        // ──────────────────────────────────────────────
        // 1.5. GET /api/courier/orders/available
        // الأوردرات غير المسندة في مغسلة الكابتن — نفس شروط Accept تماماً
        // ──────────────────────────────────────────────
        [HttpGet("orders/available")]
        public async Task<IActionResult> GetAvailableOrders(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.GetAvailableOrdersAsync(GetCourierId(), pageIndex, pageSize);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 2. PUT /api/courier/orders/{id}/accept
        // ──────────────────────────────────────────────
        [HttpPut("orders/{id}/accept")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.AcceptOrderAsync(GetCourierId(), id);

            if (!result.IsSuccess)
                return result.ErrorMessage.Contains("not found") || result.ErrorMessage.Contains("taken")
                    ? NotFound(new { message = result.ErrorMessage })
                    : BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 3. PUT /api/courier/location
        // ──────────────────────────────────────────────
        [HttpPut("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.UpdateLocationAsync(GetCourierId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 4. PUT /api/courier/orders/{id}/pickup
        // ──────────────────────────────────────────────
        [HttpPut("orders/{id}/pickup")]
        public async Task<IActionResult> ConfirmPickup(int id, [FromBody] ConfirmPickupRequest request)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.ConfirmPickupAsync(GetCourierId(), id, request);

            if (!result.IsSuccess)
                return result.ErrorMessage.Contains("not found")
                    ? NotFound(new { message = result.ErrorMessage })
                    : BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 5. PUT /api/courier/orders/{id}/deliver
        // ──────────────────────────────────────────────
        [HttpPut("orders/{id}/deliver")]
        public async Task<IActionResult> ConfirmDelivery(int id, [FromBody] ConfirmDeliveryRequest request)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.ConfirmDeliveryAsync(GetCourierId(), id, request);

            if (!result.IsSuccess)
                return result.ErrorMessage.Contains("not found")
                    ? NotFound(new { message = result.ErrorMessage })
                    : BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 6. PUT /api/courier/orders/{id}/cancel
        // ──────────────────────────────────────────────
        [HttpPut("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CourierCancelOrderRequest request)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.CancelOrderAsync(GetCourierId(), id, request);

            if (!result.IsSuccess)
                return result.ErrorMessage.Contains("not found")
                    ? NotFound(new { message = result.ErrorMessage })
                    : BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Order cancelled successfully" });
        }

        // ──────────────────────────────────────────────
        // 7. GET /api/courier/history
        // ──────────────────────────────────────────────
        [HttpGet("history")]
        public async Task<IActionResult> GetDeliveryHistory(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.GetDeliveryHistoryAsync(
                GetCourierId(), pageIndex, pageSize);
            return Ok(result);
        }

        // ──────────────────────────────────────────────
        // 8. GET /api/courier/stats
        // ──────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.GetStatsAsync(GetCourierId());

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 9. GET /api/courier/profile
        // ──────────────────────────────────────────────
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.GetProfileAsync(GetCourierId());

            if (!result.IsSuccess)
                return NotFound(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ──────────────────────────────────────────────
        // 10. PUT /api/courier/profile
        // ──────────────────────────────────────────────
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCourierProfileRequest request)
        {
            if (!IsCourier()) return Forbid();

            var result = await _courierService.UpdateProfileAsync(GetCourierId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // ============================================================
        // APP REQUIREMENTS
        // ============================================================

        [HttpGet("~/api/driver/profile")]
        public async Task<IActionResult> GetDriverProfile()
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.GetAppProfileAsync(GetCourierId());
            if (!result.IsSuccess) return NotFound(result.ErrorMessage);
            return Ok(result.Value);
        }

        [HttpPut("~/api/driver/status")]
        public async Task<IActionResult> UpdateDriverStatus([FromBody] UpdateCourierStatusRequestDto request)
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.UpdateStatusAsync(GetCourierId(), request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok();
        }

        [HttpGet("~/api/driver/stats/today")]
        public async Task<IActionResult> GetTodayStats()
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.GetTodaysStatsAsync(GetCourierId());
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok(result.Value);
        }

        [HttpGet("~/api/driver/runs/active")]
        public async Task<IActionResult> GetActiveRun()
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.GetActiveRunAsync(GetCourierId());
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok(result.Value);
        }

        [HttpPut("~/api/driver/runs/{runId}/stops/{orderId}/deliver")]
        public async Task<IActionResult> DeliverRunStop(string runId, string orderId, [FromBody] DeliverStopRequestDto request)
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.DeliverStopAsync(GetCourierId(), runId, orderId, request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok();
        }

        [HttpPost("~/api/driver/location/sync")]
        public async Task<IActionResult> SyncDriverLocation([FromBody] LocationSyncRequestDto request)
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.SyncLocationAsync(GetCourierId(), request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok();
        }

        [HttpGet("~/api/driver/earnings")]
        public async Task<IActionResult> GetDriverEarnings()
        {
            if (!IsCourier()) return Forbid();
            var result = await _courierService.GetEarningsStatsAsync(GetCourierId());
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok(result.Value);
        }
    }
}
