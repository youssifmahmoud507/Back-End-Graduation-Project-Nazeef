using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Auth
{
    public class VerifyOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }
    }

}
