using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Auth
{
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }

}
