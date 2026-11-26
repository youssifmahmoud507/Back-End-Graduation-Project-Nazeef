using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.DTO.LaundryAdmin
{
    /// <summary>
    /// بيانات الطلب اللي بتتعرض في داشبورد صاحب المغسلة
    /// بيشيل كل تفاصيل الطلب + بيانات العميل + الأيتمز
    /// </summary>
    public class LaundryOrderDto
    {
        /// <summary>رقم الطلب الفريد في الداتا بيز</summary>
        public int Id { get; set; }

        /// <summary>اسم العميل اللي عمل الطلب</summary>
        public string CustomerName { get; set; }

        /// <summary>رقم تليفون العميل للتواصل معاه لو في مشكلة</summary>
        public string CustomerPhone { get; set; }

        /// <summary>عنوان الاستلام من العميل</summary>
        public string PickupLocation { get; set; }

        /// <summary>عنوان التوصيل للعميل بعد الغسيل</summary>
        public string DeliveryLocation { get; set; }

        /// <summary>الوقت المحدد لاستلام الغسيل من العميل</summary>
        public DateTime PickupTime { get; set; }

        /// <summary>
        /// الحالة الحالية للطلب
        /// مثال: PendingConfirmation, InProgress, ReadyForDelivery
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>السعر الإجمالي للطلب بعد حساب كل الخدمات</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>تاريخ ووقت إنشاء الطلب</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>قايمة الخدمات اللي الطلب بيحتويها (غسيل، كوي، إلخ)</summary>
        public List<LaundryOrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// تفاصيل كل خدمة جوه الطلب
    /// بيتعرض جوه LaundryOrderDto في قايمة الـ Items
    /// </summary>
    public class LaundryOrderItemDto
    {
        /// <summary>اسم الخدمة (مثال: غسيل، كوي، دراي كلين)</summary>
        public string ServiceName { get; set; }

        /// <summary>عدد القطع المطلوبة من الخدمة دي</summary>
        public int Quantity { get; set; }

        /// <summary>السعر الإجمالي لهذه الخدمة (الكمية × سعر الوحدة)</summary>
        public decimal Subtotal { get; set; }
    }

}
