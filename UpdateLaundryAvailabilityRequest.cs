using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class UpdateLaundryAvailabilityRequest
    {
        [Required(ErrorMessage = "Availability is required")]
        public LaundryAvailability Availability { get; set; }
    }
}