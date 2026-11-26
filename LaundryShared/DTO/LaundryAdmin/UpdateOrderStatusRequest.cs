using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.LaundryAdmin
{
    /// <summary>
    /// الـ Request اللي صاحب المغسلة بيبعته عشان يغير حالة الطلب
    /// بيتبعت في الـ Body مع PUT /api/laundry-admin/orders/{id}/status
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        /// <summary>
        /// الحالة الجديدة المطلوب تغيير الطلب ليها
        /// مثال: Accepted, InProgress, ReadyForDelivery, Cancelled
        /// </summary>
        [Required(ErrorMessage = "NewStatus is required")]
        public OrderStatus NewStatus { get; set; }

        /// <summary>
        /// سبب الإلغاء - مطلوب بس لو الـ NewStatus = Cancelled
        /// اختياري في باقي الحالات
        /// </summary>
        public string? CancellationReason { get; set; }
    }
}
