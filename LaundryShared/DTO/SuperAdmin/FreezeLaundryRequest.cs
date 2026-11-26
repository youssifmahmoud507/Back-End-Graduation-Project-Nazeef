using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.SuperAdmin
{
    public class FreezeLaundryRequest
    {
        [MaxLength(250)]
        public string? Reason { get; set; }
    }
}
