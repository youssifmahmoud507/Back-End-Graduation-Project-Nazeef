using LaundryDomain.Models.UserModules;
using LaundryServiesAbstraction.IServices.Verification;
using LaundryShared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TicketsShared.DTO.Verification;

namespace LaundryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly IDiditVerificationService _diditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VerificationController> _logger;
        private readonly IConfiguration _configuration;

        public VerificationController(
            IDiditVerificationService diditService,
            UserManager<ApplicationUser> userManager,
            ILogger<VerificationController> logger,
            IConfiguration configuration)
        {
            _diditService = diditService;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// GET /api/verification/webhook?sessionId=xxx&status=Approved
        /// Didit redirects here after completion, then backend redirects to frontend success page.
        /// </summary>
        [HttpGet("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> DiditCallback(
            [FromQuery] string? sessionId,
            [FromQuery] string? session_id,
            [FromQuery] string? verificationSessionId,
            [FromQuery] string? frontendRedirect,
            [FromQuery] string? status)
        {
            var actualSessionId = sessionId ?? session_id ?? verificationSessionId;
            var frontendBaseUrl = _configuration["Didit:FrontendBaseUrl"] ?? "https://www.nazeef.tech";
            var redirectUrl = string.IsNullOrWhiteSpace(frontendRedirect)
                ? $"{frontendBaseUrl.TrimEnd('/')}/laundry-admin/verification/success"
                : frontendRedirect;

            if (!string.IsNullOrWhiteSpace(actualSessionId))
            {
                var syncSucceeded = await _diditService.SyncVerificationStatusAsync(actualSessionId);
                _logger.LogInformation("Didit callback session sync for {SessionId}: {SyncSucceeded}",
                    actualSessionId, syncSucceeded);
            }

            if (!string.IsNullOrWhiteSpace(actualSessionId))
            {
                redirectUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(redirectUrl, "sessionId", actualSessionId);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    redirectUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(redirectUrl, "status", status);
                }
            }

            return Redirect(redirectUrl);
        }

        /// <summary>
        /// POST /api/verification/webhook
        /// Didit server-to-server webhook endpoint.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> DiditWebhook()
        {
            Request.EnableBuffering();
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0;

            DiditWebhookPayload? payload;
            try
            {
                payload = System.Text.Json.JsonSerializer.Deserialize<DiditWebhookPayload>(rawBody, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON" });
            }

            if (payload == null)
                return BadRequest(new { message = "Empty payload" });

            try
            {
                var signature = Request.Headers["X-Signature-V2"].FirstOrDefault() ?? string.Empty;
                var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();
                var secret = _configuration["Didit:WebhookSecretKey"] ?? string.Empty;

                var success = await _diditService.ProcessWebhookAsync(payload, rawBody, signature, secret, timestamp);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to process webhook" });
                }

                return Ok(new { received = true, processed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Didit webhook");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new verification session for the current user
        /// </summary>
        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateVerificationRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check if already verified
                if (user.IsIdentityVerified)
                {
                    return BadRequest(new { message = "User is already verified" });
                }

                // Build redirect URL
                var frontendBaseUrl = _configuration["Didit:FrontendBaseUrl"] ?? "https://www.nazeef.tech";
                var redirectUrl = request.RedirectUrl ?? $"{frontendBaseUrl}/laundry-admin/verification/success";

                _logger.LogInformation("Creating verification session for user: {UserId}", userId);

                var session = await _diditService.CreateVerificationSessionAsync(userId, email, redirectUrl);

                return Ok(new
                {
                    sessionId = session.SessionId,
                    sessionToken = session.SessionToken,
                    url = session.Url,
                    status = session.Status,
                    message = "Verification session created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create verification session");
                return StatusCode(500, new { message = "Failed to create verification session", error = ex.Message });
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteVerification()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var completed = await ForceCompleteVerificationAsync(user, "frontend_manual_completion");
                if (!completed)
                {
                    return StatusCode(500, new { message = "Could not complete verification right now." });
                }

                return Ok(new
                {
                    verified = true,
                    adminApprovalStatus = user.AdminApprovalStatus?.ToString(),
                    role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete verification manually");
                return StatusCode(500, new { message = "Could not complete verification right now." });
            }
        }

        /// <summary>
        /// Gets the verification status for the current user
        /// </summary>
        [HttpPost("sync")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncVerificationStatus(
            [FromQuery] string? sessionId = null,
            [FromQuery] string? session_id = null,
            [FromQuery] string? verificationSessionId = null)
        {
            var actualSessionId = sessionId ?? session_id ?? verificationSessionId;
            if (string.IsNullOrWhiteSpace(actualSessionId))
            {
                return BadRequest(new { synced = false, message = "Verification session is missing." });
            }

            var synced = await _diditService.SyncVerificationStatusAsync(actualSessionId);
            return Ok(new { synced });
        }

        /// <summary>
        /// Gets the verification status for the current user
        /// </summary>
        // Throttle how often we call Didit's API for the same session inside GetVerificationStatus.
        // Prevents hammering Didit when the frontend polls rapidly.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _syncCooldown = new();
        private static readonly TimeSpan SyncCooldown = TimeSpan.FromSeconds(30);

        [HttpGet("status")]
        public async Task<IActionResult> GetVerificationStatus(
            [FromQuery] string? sessionId = null,
            [FromQuery] string? session_id = null,
            [FromQuery] string? verificationSessionId = null)
        {
            try
            {
                var actualSessionId = sessionId ?? session_id ?? verificationSessionId;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Only sync from Didit if not yet verified AND a session ID is present
                // AND we haven't synced this session in the last 30 s (prevents Didit hammering)
                if (!user.IsIdentityVerified && !string.IsNullOrWhiteSpace(actualSessionId))
                {
                    var now = DateTime.UtcNow;
                    var canSync = !_syncCooldown.TryGetValue(actualSessionId, out var lastSync)
                                  || (now - lastSync) >= SyncCooldown;

                    if (canSync)
                    {
                        _syncCooldown[actualSessionId] = now;
                        var syncSucceeded = await _diditService.SyncVerificationStatusAsync(actualSessionId);
                        _logger.LogInformation("Verification status sync for user {UserId} using session {SessionId}: {SyncSucceeded}",
                            userId, actualSessionId, syncSucceeded);

                        user = await _userManager.FindByIdAsync(userId);
                        if (user == null)
                        {
                            return NotFound(new { message = "User not found" });
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Skipping Didit sync for session {SessionId} — last synced {Elapsed:F0}s ago",
                            actualSessionId, (now - lastSync).TotalSeconds);
                    }
                }

                return Ok(new
                {
                    isVerified = user.IsIdentityVerified,
                    isEmailVerified = user.IsEmailVerified,
                    adminApprovalStatus = user.AdminApprovalStatus?.ToString(),
                    role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get verification status");
                return StatusCode(500, new { message = "Failed to get verification status" });
            }
        }

        /// <summary>
        /// Checks if the current user needs verification
        /// </summary>
        [HttpGet("required")]
        [AllowAnonymous]
        public async Task<IActionResult> IsVerificationRequired([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // If user is already verified properly, return false
                if (user.IsIdentityVerified)
                {
                    return Ok(new
                    {
                        userId = user.Id,
                        needsVerification = false,
                        isVerified = true,
                        role = user.Role.ToString()
                    });
                }

                // Only LaundryAdmins need identity verification
                var needsVerification = user.Role == LaundryShared.Enums.UserRole.LaundryAdmin
                    && !user.IsIdentityVerified;

                return Ok(new
                {
                    userId = user.Id,
                    needsVerification = needsVerification,
                    isVerified = user.IsIdentityVerified,
                    role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check verification requirement");
                return StatusCode(500, new { message = "Failed to check verification requirement" });
            }
        }

        private async Task<bool> ForceCompleteVerificationAsync(ApplicationUser user, string source)
        {
            if (user.Role != UserRole.LaundryAdmin)
            {
                _logger.LogWarning("Manual verification completion was requested for non-laundry-admin user {UserId}", user.Id);
                return false;
            }

            if (user.IsIdentityVerified && user.AdminApprovalStatus == LaundryAdminApprovalStatus.Approved)
            {
                return true;
            }

            user.IsIdentityVerified = true;
            user.AdminApprovalStatus = LaundryAdminApprovalStatus.Approved;
            user.AdminRejectionReason = null;

            if (string.IsNullOrWhiteSpace(user.CommercialRegisterDocumentUrl))
            {
                user.CommercialRegisterDocumentUrl = $"manual_verification_{source}";
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to manually verify user {UserId}: {Errors}",
                    user.Id,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("User {UserId} was manually marked as verified via {Source}", user.Id, source);
            return true;
        }
    }

    public class CreateVerificationRequest
    {
        public string? RedirectUrl { get; set; }
    }
}
