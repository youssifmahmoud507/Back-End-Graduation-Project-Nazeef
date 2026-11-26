using LaundryDomain.Models.Common;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Models.OrderModules;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaundryDomain.Models.UserModules
{
    public class UserCoupon : BaseEntity<UserCoupon, int>
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int CouponId { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [ForeignKey("CouponId")]
        public Coupon Coupon { get; set; }
    }
}