using System.Text.Json.Serialization;

namespace TicketsShared.DTO.Verification
{
    public class CreateSessionRequest
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; }

        [JsonPropertyName("vendor_data")]
        public string VendorData { get; set; }

        [JsonPropertyName("callback")]
        public string Callback { get; set; }

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonPropertyName("callback_method")]
        public string CallbackMethod { get; set; } = "both";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "ar";

        [JsonPropertyName("features")]
        public DiditFeatures Features { get; set; }
    }

    public class DiditFeatures
    {
        [JsonPropertyName("allow_desktop")]
        public bool AllowDesktop { get; set; }

        [JsonPropertyName("desktop_sync")]
        public bool DesktopSync { get; set; }

        [JsonPropertyName("show_exit_button")]
        public bool ShowExitButton { get; set; }

        [JsonPropertyName("show_navigation_button")]
        public bool ShowNavigationButton { get; set; }

        [JsonPropertyName("disable_redirection_delay")]
        public bool DisableRedirectionDelay { get; set; }
    }

    public class CreateSessionResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("session_token")]
        public string SessionToken { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class DiditWebhookPayload
    {
        /// <summary>
        /// Unique per delivery attempt — used as idempotency key (Didit V3 docs).
        /// </summary>
        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("vendor_data")]
        public string VendorData { get; set; }

        [JsonPropertyName("webhook_type")]
        public string WebhookType { get; set; }

        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; }

        /// <summary>Unix timestamp seconds — used for replay protection.</summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }
    }
}