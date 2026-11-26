using System;
using System.ComponentModel.DataAnnotations;

namespace LaundryShared.DTO.SuperAdmin
{
    public class CreateCouponRequest
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public bool IsPercentage { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal DiscountValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }

        public bool IsActive { get; set; } = true;

        public int? LaundryId { get; set; }
    }
}
