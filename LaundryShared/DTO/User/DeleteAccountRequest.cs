using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.User
{
    public class DeleteAccountRequest
    {
        [Required]
        public string Password { get; set; }

        public string? Reason { get; set; }
    }
}
