using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.SuperAdmin
{
    namespace LaundryShared.DTO.SuperAdmin
    {
        /// <summary>
        /// بيانات المغسلة اللي Super Admin بيشوفها في لوحة التحكم
        /// بيشوف فيها كل المغاسل وحالتها
        /// </summary>
        public class LaundryApprovalDto
        {
            /// <summary>رقم المغسلة</summary>
            public int Id { get; set; }

            /// <summary>اسم المغسلة</summary>
            public string Name { get; set; }

            /// <summary>عنوان المغسلة</summary>
            public string Address { get; set; }

            /// <summary>صورة المغسلة</summary>
            public string? ImageUrl { get; set; }

            /// <summary>حالة المغسلة (Active / Inactive)</summary>
            public LaundryStatus Status { get; set; }

            /// <summary>حالة الاستقبال (Open / Closed / Busy)</summary>
            public LaundryAvailability Availability { get; set; }

            /// <summary>متوسط التقييم</summary>
            public double AverageRating { get; set; }

            /// <summary>تاريخ التسجيل</summary>
            public DateTime CreatedAt { get; set; }
        }
    }
}