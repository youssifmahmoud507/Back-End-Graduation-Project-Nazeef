using LaundryDomain.Models.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaundryDomain.Models.OrderModules
{
    public class Coupon : BaseEntity<Coupon, int>
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        public bool IsPercentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? UsageLimit { get; set; }

        public int CurrentUsage { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public int? LaundryId { get; set; } // Null if it's a global coupon
    }
}