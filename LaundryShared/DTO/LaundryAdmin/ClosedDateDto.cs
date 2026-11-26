using System;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class ClosedDateDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime Date { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
