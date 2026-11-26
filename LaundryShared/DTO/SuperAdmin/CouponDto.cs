using System;

namespace LaundryShared.DTO.SuperAdmin
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? UsageLimit { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsActive { get; set; }
        public int? LaundryId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
