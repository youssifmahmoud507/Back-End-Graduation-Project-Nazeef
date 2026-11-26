using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.SuperAdmin
{
    /// <summary>
    /// بيانات اليوزر اللي Super Admin بيشوفها في لوحة التحكم
    /// </summary>
    public class UserManagementDto
    {
        /// <summary>رقم اليوزر</summary>
        public string Id { get; set; }

        /// <summary>اسم اليوزر</summary>
        public string Name { get; set; }

        /// <summary>الإيميل</summary>
        public string Email { get; set; }

        /// <summary>رقم التليفون</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>الدور (Customer / Courier / LaundryAdmin)</summary>
        public string Role { get; set; }

        /// <summary>الأكونت شغال ولا موقوف</summary>
        public bool IsActive { get; set; }

        /// <summary>تاريخ التسجيل</summary>
        public DateTime CreatedAt { get; set; }
    }
}
