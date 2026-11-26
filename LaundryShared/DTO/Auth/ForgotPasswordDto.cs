using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

}
