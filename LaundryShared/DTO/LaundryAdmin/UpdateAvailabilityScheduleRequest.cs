using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class UpdateAvailabilityScheduleRequest
    {
        [Required]
        [System.Text.Json.Serialization.JsonPropertyName("days")]
        public List<AvailabilityScheduleDayDto> Days { get; set; } = new();
    }
}
