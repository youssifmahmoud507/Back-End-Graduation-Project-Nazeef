using System.Text.Json.Serialization;

namespace LaundryShared.DTO.PaymentKashier
{
    public class KashierWebhookDto
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("data")]
        public KashierWebhookDataDto Data { get; set; }
    }

    public class KashierWebhookDataDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("merchantOrderId")]
        public string MerchantOrderId { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}
