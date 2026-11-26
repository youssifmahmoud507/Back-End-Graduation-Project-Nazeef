using System;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class UpdateClosedDateRequest
    {
        [Required]
        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [MaxLength(200)]
        [System.Text.Json.Serialization.JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
