using System.ComponentModel.DataAnnotations;
using LaundryShared.Enums;

namespace LaundryShared.DTO.Auth
{
    /// <summary>
    /// الـ Token اللي بييجي من Google بعد ما اليوزر يعمل Login
    /// الـ Frontend بيبعته للـ Backend عشان يتحقق منه
    /// </summary>
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google token is required")]
        public string IdToken { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }
    }
}