using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.SuperAdmin
{
    /// <summary>
    /// الـ Request اللي Super Admin بيبعته عشان يوقف أو يفعّل أكونت يوزر
    /// </summary>
    public class UpdateUserStatusRequest
    {
        /// <summary>
        /// true  = الأكونت شغال
        /// false = الأكونت موقوف
        /// </summary>
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }

        /// <summary>سبب الإيقاف - مطلوب لو IsActive = false</summary>
        public string? Reason { get; set; }
    }
}
