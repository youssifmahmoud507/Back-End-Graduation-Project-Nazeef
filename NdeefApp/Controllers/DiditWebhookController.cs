using LaundryServiesAbstraction.IServices.Verification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TicketsShared.DTO.Verification;

namespace LaundryApi.Controllers
{
    [ApiController]
    [Route("api/webhook/didit")]
    public class DiditWebhookController : ControllerBase
    {
        private readonly IDiditVerificationService _diditService;
        private readonly ILogger<DiditWebhookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        public DiditWebhookController(
            IDiditVerificationService diditService,
            ILogger<DiditWebhookController> logger,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _diditService = diditService;
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Receives webhook notifications from Didit when verification is completed
        /// </summary>
        [HttpPost]

        [Consumes("application/json")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // Enable buffering so we can read the raw body AND still deserialize it
            Request.EnableBuffering();
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0; // rewind for any downstream middleware

            DiditWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<DiditWebhookPayload>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Didit webhook: failed to parse JSON body");
                return BadRequest(new { message = "Invalid JSON" });
            }

            if (payload == null)
                return BadRequest(new { message = "Empty payload" });

            try
            {
                // Log ALL request details for debugging
                _logger.LogInformation("=== WEBHOOK RECEIVED ===");
                _logger.LogInformation("Headers: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}")));
                _logger.LogInformation("SessionId: {SessionId}", payload.SessionId);
                _logger.LogInformation("EventId: {EventId}", payload.EventId);
                _logger.LogInformation("WebhookType: {WebhookType}", payload.WebhookType);
                _logger.LogInformation("Status: {Status}", payload.Status);
                _logger.LogInformation("VendorData: {VendorData}", payload.VendorData);
                _logger.LogInformation("========================");

                // Idempotency: dedupe on parsed.event_id (every delivery attempt has a unique id)
                var eventId = payload.EventId ?? $"{payload.SessionId}_{payload.WebhookType}_{payload.Timestamp}";
                if (!string.IsNullOrEmpty(eventId))
                {
                    if (_memoryCache.TryGetValue($"didit_webhook_{eventId}", out _))
                    {
                        _logger.LogInformation("Webhook event {EventId} was already processed successfully. Returning 200 OK early.", eventId);
                        return Ok(new { message = "Already processed" });
                    }
                }

                // Read Didit V3 signature headers (X-Signature-V2 is the recommended header)
                var signature = Request.Headers["X-Signature-V2"].FirstOrDefault() ?? string.Empty;
                var timestamp = Request.Headers["X-Timestamp"].FirstOrDefault();
                var secret = _configuration["Didit:WebhookSecretKey"] ?? string.Empty;

                // Process the webhook (rawBody passed for canonical HMAC; timestamp for replay protection)
                var success = await _diditService.ProcessWebhookAsync(payload, rawBody, signature, secret, timestamp);

                if (!success)
                {
                    _logger.LogWarning("Failed to process webhook for session: {SessionId}", payload.SessionId);
                    return BadRequest(new { message = "Failed to process webhook" });
                }

                // Mark processed in cache for 24 hours to prevent duplicate processing
                if (!string.IsNullOrEmpty(eventId))
                {
                    _memoryCache.Set($"didit_webhook_{eventId}", true, TimeSpan.FromHours(24));
                }

                // Return 200 OK to acknowledge receipt
                return Ok(new
                {
                    message = "Webhook processed successfully",
                    sessionId = payload.SessionId,
                    status = payload.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Didit webhook for session: {SessionId}", payload?.SessionId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Backward-compatible GET callback endpoint.
        /// If Didit redirects to /api/webhook/didit, forward user to frontend success page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CallbackRedirect(
            [FromQuery] string? sessionId,
            [FromQuery] string? session_id,
            [FromQuery] string? verificationSessionId,
            [FromQuery] string? frontendRedirect,
            [FromQuery] string? status)
        {
            var actualSessionId = sessionId ?? session_id ?? verificationSessionId;
            var frontendBaseUrl = _configuration["Didit:FrontendBaseUrl"]
                                  ?? _configuration["FrontendUrl"]
                                  ?? "https://www.nazeef.tech";
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
                redirectUrl = AppendQueryParameter(redirectUrl, "sessionId", actualSessionId);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    redirectUrl = AppendQueryParameter(redirectUrl, "status", status);
                }
            }

            return Redirect(redirectUrl);
        }

        private static string AppendQueryParameter(string url, string key, string value)
        {
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        }
    }
}
