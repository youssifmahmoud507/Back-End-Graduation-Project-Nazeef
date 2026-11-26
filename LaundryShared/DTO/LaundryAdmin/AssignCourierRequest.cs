using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class AssignCourierRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
