using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class PayCommissionRequest
    {
        [Required]
        public int LaundryId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}
