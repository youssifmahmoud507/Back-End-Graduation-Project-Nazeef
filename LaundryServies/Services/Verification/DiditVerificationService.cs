using LaundryDomain.Models.UserModules;
using LaundryServiesAbstraction.IServices.Verification;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TicketsShared.DTO.Verification;

namespace LaundryServies.Services.Verification
{
    public class DiditVerificationService : IDiditVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiditVerificationService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public DiditVerificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<DiditVerificationService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
        }

        private string ApiKey => _configuration["Didit:ApiKey"]!;
        private string BaseUrl => _configuration["Didit:BaseUrl"] ?? "https://verification.didit.me";
        private string WorkflowId => _configuration["Didit:WorkflowId"]!;
        private string WebhookBaseUrl => _configuration["Didit:WebhookBaseUrl"]!;
        private string FrontendBaseUrl => _configuration["Didit:FrontendBaseUrl"] ?? "https://www.nazeef.tech";

        public async Task<CreateSessionResponse> CreateVerificationSessionAsync(string userId, string email, string? redirectUrl = null)
        {
            try
            {
                var uniqueVendorData = $"{userId}_{Guid.NewGuid():N}";
                var finalRedirectUrl = redirectUrl ?? $"{FrontendBaseUrl}/laundry-admin/verification/success";

                var request = new CreateSessionRequest
                {
                    WorkflowId = WorkflowId,
                    VendorData = uniqueVendorData,
                    Callback = finalRedirectUrl, // Didit redirects here and appends ?session_id=...
                    RedirectUrl = finalRedirectUrl,
                    CallbackMethod = "both",
                    Language = "ar",
                    Features = new DiditFeatures
                    {
                        AllowDesktop = true,
                        DesktopSync = true,
                        ShowExitButton = true,
                        ShowNavigationButton = true,
                        DisableRedirectionDelay = true
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v3/session/")
                {
                    Content = content,
                    Headers =
                    {
                        { "X-Api-Key", ApiKey }
                    }
                };

                _logger.LogInformation("Creating Didit verification session for user: {UserId}", userId);
                if (!string.IsNullOrWhiteSpace(redirectUrl))
                {
                    _logger.LogInformation("Frontend requested verification return URL {RedirectUrl}, but backend callback will be used first.", redirectUrl);
                }

                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Didit API error: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    throw new Exception($"Didit API error: {response.StatusCode} - {responseContent}");
                }

                var sessionResponse = JsonSerializer.Deserialize<CreateSessionResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Created verification session: {SessionId} for user: {UserId}",
                    sessionResponse?.SessionId, userId);

                return sessionResponse!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create verification session for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ProcessWebhookAsync(DiditWebhookPayload payload, string rawBody, string signatureV2, string secret, string? timestampHeader = null)
        {
            try
            {
                _logger.LogInformation("Processing Didit webhook for session: {SessionId}, status: {Status}",
                    payload.SessionId, payload.Status);

                // ── Replay protection (±5 min) ───────────────────────────────────────
                // Prefer X-Timestamp header; fall back to payload.Timestamp
                var tsSource = timestampHeader ?? payload.Timestamp?.ToString();
                if (tsSource != null && long.TryParse(tsSource, out var ts))
                {
                    var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
                    if (age > 300)
                    {
                        _logger.LogWarning("Didit webhook replay detected for session {SessionId}: age={Age}s", payload.SessionId, age);
                        return false;
                    }
                }

                // ── Signature validation (X-Signature-V2 canonical algorithm) ────────
                if (!string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(signatureV2))
                {
                    if (!ValidateWebhookSignatureV2(rawBody, signatureV2, secret))
                    {
                        _logger.LogWarning("Invalid X-Signature-V2 for session {SessionId}. Falling back to direct Didit API fetch.", payload.SessionId);

                        var directSession = await GetSessionStatusAsync(payload.SessionId);
                        if (directSession == null)
                        {
                            _logger.LogError("Could not retrieve session {SessionId} from Didit API. Rejecting.", payload.SessionId);
                            return false;
                        }
                        payload.Status = directSession.Status;
                        _logger.LogInformation("Session {SessionId} status from Didit API: {Status}", payload.SessionId, payload.Status);
                    }
                }

                // ── Extract userId from vendor_data (format: userId_guid) ────────────
                var userId = ExtractUserId(payload.VendorData);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Could not extract user ID from vendor_data: {VendorData}", payload.VendorData);
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User not found: {UserId}", userId);
                    return false;
                }

                return await ApplyVerificationStatusAsync(user, payload.Status, payload.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process webhook for session: {SessionId}", payload.SessionId);
                return false;
            }
        }

        public async Task<bool> SyncVerificationStatusAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v3/session/{sessionId}/decision/")
                {
                    Headers =
                    {
                        { "X-Api-Key", ApiKey }
                    }
                };

                var response = await _httpClient.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to sync session status from Didit: {StatusCode} - {Error}",
                        response.StatusCode, content);
                    return false;
                }

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                var status = GetJsonString(root, "status", "decision", "verification_status");
                var vendorData = GetJsonString(root, "vendor_data", "vendorData");
                var resolvedSessionId = GetJsonString(root, "session_id", "sessionId", "id") ?? sessionId;

                if (string.IsNullOrWhiteSpace(vendorData))
                {
                    _logger.LogWarning("Didit session {SessionId} did not include vendor_data; cannot sync user state.", sessionId);
                    return false;
                }

                var userId = ExtractUserId(vendorData);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Didit session {SessionId} returned invalid vendor_data: {VendorData}", sessionId, vendorData);
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Didit session {SessionId} resolved to missing user: {UserId}", sessionId, userId);
                    return false;
                }

                return await ApplyVerificationStatusAsync(user, status, resolvedSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync Didit verification state for session: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<CreateSessionResponse?> GetSessionStatusAsync(string sessionId)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v3/session/{sessionId}/decision/")
                {
                    Headers =
                    {
                        { "X-Api-Key", ApiKey }
                    }
                };

                var response = await _httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get session status: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CreateSessionResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session status for: {SessionId}", sessionId);
                return null;
            }
        }

        /// <summary>
        /// Validates the Didit X-Signature-V2 header using the canonical algorithm:
        /// Parse raw JSON → shortenFloats → sortKeys → JSON.stringify (unescaped Unicode) → HMAC-SHA256.
        /// MUST operate on the raw request body, NOT on a re-serialized C# DTO.
        /// </summary>
        private static bool ValidateWebhookSignatureV2(string rawBody, string signatureV2, string secret)
        {
            if (string.IsNullOrEmpty(signatureV2) || string.IsNullOrEmpty(secret))
                return string.IsNullOrEmpty(signatureV2); // skip if both absent

            try
            {
                // 1. Parse raw body as JsonNode (preserves all fields, including ones not in DTO)
                var node = JsonNode.Parse(rawBody);
                if (node == null) return false;

                // 2. Apply shortenFloats + sortKeys recursively
                var normalised = SortKeys(node);

                // 3. Serialize with UnsafeRelaxedJsonEscaping (unescaped Unicode — same as JS JSON.stringify default)
                var canonical = JsonSerializer.Serialize(
                    normalised,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }
                );

                // 4. HMAC-SHA256
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var computedHex = Convert.ToHexString(computedBytes).ToLowerInvariant();

                // 5. Constant-time comparison
                var sigBytes    = Encoding.UTF8.GetBytes(signatureV2.ToLowerInvariant());
                var expectBytes = Encoding.UTF8.GetBytes(computedHex);
                return CryptographicOperations.FixedTimeEquals(sigBytes, expectBytes);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Recursively sorts the keys of a JsonNode object (arrays preserved in order).
        /// Whole-number floats are converted to integers (shortenFloats) as required by Didit V2 canonical spec.
        /// </summary>
        private static JsonNode? SortKeys(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                var sorted = new JsonObject();
                foreach (var key in obj.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal))
                {
                    sorted[key] = SortKeys(obj[key]?.DeepClone());
                }
                return sorted;
            }

            if (node is JsonArray arr)
            {
                var sortedArr = new JsonArray();
                foreach (var item in arr)
                    sortedArr.Add(SortKeys(item?.DeepClone()));
                return sortedArr;
            }

            // shortenFloats: whole-number doubles → integers
            if (node is JsonValue val && val.TryGetValue<double>(out var d) && d % 1 == 0 && !double.IsInfinity(d) && !double.IsNaN(d))
                return JsonValue.Create((long)d);

            return node;
        }

        private static string ExtractUserId(string vendorData)
        {
            if (string.IsNullOrEmpty(vendorData))
                return vendorData;

            var underscoreIndex = vendorData.IndexOf('_');
            return underscoreIndex > 0 ? vendorData.Substring(0, underscoreIndex) : vendorData;
        }

        private async Task<bool> ApplyVerificationStatusAsync(ApplicationUser user, string? status, string? sessionId)
        {
            // Didit V3: compare case-sensitively against exact status strings.
            // See: https://docs.didit.me/integration/verification-statuses
            switch (status)
            {
                case "Approved":
                    user.IsIdentityVerified = true;
                    if (user.Role == LaundryShared.Enums.UserRole.LaundryAdmin)
                    {
                        // Set to Approved (not just Pending) so downstream checks pass
                        user.AdminApprovalStatus = LaundryShared.Enums.LaundryAdminApprovalStatus.Approved;
                        if (!string.IsNullOrWhiteSpace(sessionId))
                        {
                            user.CommercialRegisterDocumentUrl = $"didit_session_{sessionId}";
                        }
                    }
                    var approveResult = await _userManager.UpdateAsync(user);
                    if (!approveResult.Succeeded)
                    {
                        _logger.LogError("Failed to update user verification status to Approved: {Errors}",
                            string.Join(", ", approveResult.Errors.Select(e => e.Description)));
                        return false;
                    }
                    _logger.LogInformation("User {UserId} approved by Didit (session {SessionId})", user.Id, sessionId);
                    return true;

                case "Declined":
                    _logger.LogWarning("User {UserId} identity verification Declined by Didit (session {SessionId})", user.Id, sessionId);
                    // Don't set IsIdentityVerified — user stays unverified.
                    return true;

                case "In Review":
                    _logger.LogInformation("User {UserId} identity verification is In Review (session {SessionId})", user.Id, sessionId);
                    return true;

                case "Resubmitted":
                    _logger.LogInformation("User {UserId} identity verification Resubmitted (session {SessionId})", user.Id, sessionId);
                    return true;

                case "Abandoned":
                    _logger.LogInformation("User {UserId} identity verification Abandoned (session {SessionId})", user.Id, sessionId);
                    return true;

                case "Expired":
                    _logger.LogInformation("User {UserId} identity verification Expired (session {SessionId})", user.Id, sessionId);
                    return true;

                case "Kyc Expired":
                    // User was verified but their KYC has aged out — mark as unverified so they restart.
                    user.IsIdentityVerified = false;
                    var kycExpiredResult = await _userManager.UpdateAsync(user);
                    if (!kycExpiredResult.Succeeded)
                    {
                        _logger.LogError("Failed to mark user {UserId} as KYC Expired: {Errors}",
                            user.Id, string.Join(", ", kycExpiredResult.Errors.Select(e => e.Description)));
                        return false;
                    }
                    _logger.LogInformation("User {UserId} KYC Expired — marked unverified (session {SessionId})", user.Id, sessionId);
                    return true;

                case "In Progress":
                case "Not Started":
                case "Awaiting User":
                    _logger.LogInformation("User {UserId} verification status: {Status} (session {SessionId})", user.Id, status, sessionId);
                    return true;

                default:
                    _logger.LogWarning("Unrecognised Didit status '{Status}' for user {UserId} (session {SessionId}) — no action taken.",
                        status, user.Id, sessionId);
                    return true;
            }
        }

        private static string? GetJsonString(JsonElement element, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }

            return null;
        }
    }
}
