using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class SetupLaundryRequest
    {
        [Required(ErrorMessage = "Laundry Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Laundry Address is required")]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public decimal Longitude { get; set; }

        public string? ImageUrl { get; set; }
    }
}
