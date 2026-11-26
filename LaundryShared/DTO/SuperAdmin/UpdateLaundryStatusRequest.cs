using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.SuperAdmin
{

    namespace LaundryShared.DTO.SuperAdmin
    {
        /// <summary>
        /// الـ Request اللي Super Admin بيبعته عشان يوافق أو يوقف مغسلة
        /// </summary>
        public class UpdateLaundryStatusRequest
        {
            /// <summary>
            /// الحالة الجديدة للمغسلة
            /// Active  = موافق عليها وشغالة
            /// Inactive = موقوفة
            /// </summary>
            [Required(ErrorMessage = "Status is required")]
            public LaundryStatus Status { get; set; }

            /// <summary>سبب الإيقاف - مطلوب لو Status = Inactive</summary>
            public string? Reason { get; set; }
        }
    }
}