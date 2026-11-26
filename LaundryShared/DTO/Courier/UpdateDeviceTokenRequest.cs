using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Courier
{
    public class UpdateDeviceTokenRequest
    {
        [Required]
        public string DeviceToken { get; set; } = string.Empty;
    }
}