using LaundryServiesAbstraction.IServices.BundleModule;
using LaundryShared.DTO.BundleModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly IBundleService _bundleService;
        private readonly IBundleOrderService _bundleOrderService;

        public BundlesController(
            IBundleService bundleService,
            IBundleOrderService bundleOrderService)
        {
            _bundleService = bundleService;
            _bundleOrderService = bundleOrderService;
        }

        // ── Helper — same pattern as existing controllers ──────────
        private string GetAdminId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // ============================================================
        // PUBLIC ENDPOINTS — no auth required
        // ============================================================

        /// <summary>
        /// GET /api/bundles/laundry/{laundryId}
        /// Returns all active bundles available at a specific laundry
        /// Visible to unauthenticated users as marketing material
        /// </summary>
        [HttpGet("laundry/{laundryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBundlesByLaundry(int laundryId)
        {
            var result = await _bundleService.GetBundlesByLaundryAsync(laundryId);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/bundles/{bundleId}
        /// Returns details of a specific bundle
        /// </summary>
        [HttpGet("{bundleId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBundleById(Guid bundleId)
        {
            var result = await _bundleService.GetBundleByIdAsync(bundleId);

            if (result == null)
                return NotFound(new { Message = "Bundle not found." });

            return Ok(result);
        }

        // ============================================================
        // CUSTOMER ENDPOINTS
        // ============================================================

        /// <summary>
        /// POST /api/bundles/order
        /// Customer places a bundle order
        /// </summary>
        [HttpPost("order")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PlaceBundleOrder([FromBody] PlaceBundleOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAdminId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleOrderService.PlaceBundleOrderAsync(request, userId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return StatusCode(201, result.Value);
        }

        /// <summary>
        /// GET /api/bundles/orders/{orderId}/metadata
        /// Returns bundle metadata for a specific order
        /// </summary>
        [HttpGet("orders/{orderId}/metadata")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetBundleMetadataByOrder(int orderId)
        {
            var result = await _bundleOrderService.GetBundleMetadataByOrderAsync(orderId);

            if (result == null)
                return NotFound(new { Message = "Bundle metadata not found for this order." });

            return Ok(result);
        }

        // ============================================================
        // SUPERADMIN ENDPOINTS
        // ============================================================

        /// <summary>
        /// GET /api/bundles/admin/all
        /// Returns all bundles (paged, all statuses) — SuperAdmin only
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAllBundles([FromQuery] BundleSearchParams searchParams)
        {
            var result = await _bundleService.GetAllBundlesAsync(searchParams);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/bundles/admin
        /// SuperAdmin creates a platform-wide bundle directly
        /// </summary>
        [HttpPost("admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.CreateBundleAsync(request, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return StatusCode(201, result.Value);
        }

        /// <summary>
        /// PUT /api/bundles/admin/{bundleId}
        /// SuperAdmin directly edits a bundle (no approval needed)
        /// </summary>
        [HttpPut("admin/{bundleId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateBundle(
            Guid bundleId, [FromBody] UpdateBundleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.UpdateBundleAsync(bundleId, request, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// PATCH /api/bundles/admin/{bundleId}/status
        /// SuperAdmin changes bundle status
        /// </summary>
        [HttpPatch("admin/{bundleId}/status")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetBundleStatus(
            Guid bundleId, [FromBody] SetBundleStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.SetBundleStatusAsync(
                bundleId, request.NewStatus, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Bundle status updated successfully." });
        }

        /// <summary>
        /// GET /api/bundles/admin/approvals
        /// SuperAdmin sees all pending bundle approval requests
        /// </summary>
        [HttpGet("admin/approvals")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10)
        {
            var result = await _bundleService.GetPendingApprovalsAsync(
                pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/bundles/admin/approvals/review
        /// SuperAdmin approves or rejects a bundle proposal or edit request
        /// </summary>
        [HttpPost("admin/approvals/review")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ReviewBundleApproval(
            [FromBody] ReviewBundleApprovalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.ReviewBundleApprovalAsync(request, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Bundle approval reviewed successfully." });
        }

        // ============================================================
        // LAUNDRYADMIN ENDPOINTS
        // ============================================================

        /// <summary>
        /// GET /api/bundles/laundry-admin/my-bundles
        /// LaundryAdmin sees all their own bundles regardless of status
        /// </summary>
        [HttpGet("laundry-admin/my-bundles")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> GetMyLaundryBundles()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.GetMyLaundryBundlesAsync(adminId);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/bundles/laundry-admin/propose
        /// LaundryAdmin proposes a new bundle — goes to PendingApproval
        /// </summary>
        [HttpPost("laundry-admin/propose")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> ProposeBundle(
            [FromBody] ProposeBundleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.ProposeBundleAsync(request, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return StatusCode(201, result.Value);
        }

        /// <summary>
        /// POST /api/bundles/laundry-admin/propose-edit
        /// LaundryAdmin proposes an edit to an existing approved bundle
        /// Existing bundle stays live until SuperAdmin approves the edit
        /// </summary>
        [HttpPost("laundry-admin/propose-edit")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> ProposeEditBundle(
            [FromBody] ProposeEditBundleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.ProposeEditBundleAsync(request, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return StatusCode(201, result.Value);
        }

        /// <summary>
        /// GET /api/bundles/laundry-admin/approval-requests
        /// LaundryAdmin sees their own proposal/edit request history
        /// </summary>
        [HttpGet("laundry-admin/approval-requests")]
        [Authorize(Roles = "LaundryAdmin")]
        public async Task<IActionResult> GetMyApprovalRequests()
        {
            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.GetMyApprovalRequestsAsync(adminId);
            return Ok(result);
        }

        // ============================================================
        // SHARED ENDPOINT — Both SuperAdmin & LaundryAdmin
        // ============================================================

        /// <summary>
        /// PATCH /api/bundles/{bundleId}/deactivate
        /// Both SuperAdmin and LaundryAdmin can deactivate a bundle
        /// Ownership rules enforced in service layer:
        /// - SuperAdmin can deactivate any bundle
        /// - LaundryAdmin can only deactivate their own laundry bundles
        /// </summary>
        [HttpPatch("{bundleId}/deactivate")]
        [Authorize(Roles = "SuperAdmin,LaundryAdmin")]
        public async Task<IActionResult> DeactivateBundle(
            Guid bundleId, [FromBody] DeactivateBundleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized(new { Message = "User not authenticated." });

            var result = await _bundleService.DeactivateBundleAsync(bundleId, adminId);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Bundle deactivated successfully." });
        }
    }
}
