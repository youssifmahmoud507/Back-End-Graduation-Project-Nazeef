using LaundryDomain.Models.Common;
using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.OrderModules
{
    public class Payment : BaseEntity<Payment, int>
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        [Required]
        [MaxLength(30)]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Required]
        [MaxLength(50)]
        public PaymentMethod PaymentMethod { get; set; }

        // FK (1-to-1)
        [Required]
        public int OrderId { get; set; }

        // Navigation Property
        public Order Order { get; set; }
    }
}
