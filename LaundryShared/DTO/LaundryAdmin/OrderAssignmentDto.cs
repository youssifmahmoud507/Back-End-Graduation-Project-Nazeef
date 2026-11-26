using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class AssignCourierRequestDto
    {
        [Required]
        public int OrderId { get; set; }

        public string? CourierId { get; set; } // Provide either ID...
        public string? CourierPhone { get; set; } // ...or Phone to assign explicitly
    }

    public class BroadcastOrderRequestDto
    {
        [Required]
        public int OrderId { get; set; }

        public double RadiusKm { get; set; } = 5.0; // Default broadcast radius
    }

    public class AvailableCourierDto
    {
        public string CourierId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public double Rating { get; set; }
    }
}