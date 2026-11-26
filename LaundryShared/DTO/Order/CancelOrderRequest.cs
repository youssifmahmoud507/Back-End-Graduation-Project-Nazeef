using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.Order
{
    public class CancelOrderRequest
    {
        [Required]
        [MaxLength(500)]
        public string CancellationReason { get; set; }
    }
}

