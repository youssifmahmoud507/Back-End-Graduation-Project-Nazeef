    using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class UpdateLaundryAvailabilityRequest
    {
        [Required(ErrorMessage = "Availability is required")]
        [EnumDataType(typeof(LaundryAvailability), ErrorMessage = "Availability value is invalid")]
        public LaundryAvailability Availability { get; set; }
    }
}