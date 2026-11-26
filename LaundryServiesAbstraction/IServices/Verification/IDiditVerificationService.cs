using TicketsShared.DTO.Verification;

namespace LaundryServiesAbstraction.IServices.Verification
{
    public interface IDiditVerificationService
    {
        /// <summary>
        /// Creates a verification session for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="email">User email</param>
        /// <param name="redirectUrl">Optional redirect URL after verification</param>
        /// <returns>Session response with verification URL</returns>
        Task<CreateSessionResponse> CreateVerificationSessionAsync(string userId, string email, string? redirectUrl = null);

        /// <summary>
        /// Processes webhook callback from Didit
        /// </summary>
        /// <param name="payload">Webhook payload (already parsed)</param>
        /// <param name="rawBody">The raw JSON body string — required for canonical X-Signature-V2 computation</param>
        /// <param name="signatureV2">Value of the X-Signature-V2 header</param>
        /// <param name="secret">DIDIT_WEBHOOK_SECRET</param>
        /// <param name="timestampHeader">Value of the X-Timestamp header (Unix seconds string)</param>
        /// <returns>True if processed successfully</returns>
        Task<bool> ProcessWebhookAsync(DiditWebhookPayload payload, string rawBody, string signatureV2, string secret, string? timestampHeader = null);

        /// <summary>
        /// Gets the status of a verification session
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <returns>Session response or null if not found</returns>
        Task<CreateSessionResponse?> GetSessionStatusAsync(string sessionId);

        /// <summary>
        /// Re-checks a session directly with Didit and updates the linked user when possible.
        /// This is used as a fallback when the webhook is delayed or missed.
        /// </summary>
        /// <param name="sessionId">The Didit session ID</param>
        /// <returns>True when the session was checked successfully, even if not approved yet</returns>
        Task<bool> SyncVerificationStatusAsync(string sessionId);
    }
}
