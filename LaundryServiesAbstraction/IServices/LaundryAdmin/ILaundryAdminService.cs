using LaundryShared.Common;
using LaundryShared.DTO.LaundryAdmin;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.LaundryAdmin
{
    /// <summary>
    /// عقد الخدمات الخاصة بصاحب المغسلة
    /// بيحدد كل العمليات اللي يقدر يعملها على مغسلته وطلباته
    /// </summary>
    public interface ILaundryAdminService
    {
        /// <summary>
        /// بيسجل بيانات المغسلة الجديدة الخاصة بالـ Admin ده لأول مرة
        /// </summary>
        Task<Result<bool>> SetupLaundryProfileAsync(string adminId, SetupLaundryRequest request);

        /// <summary>
        /// بيعدل بيانات المغسلة الحالية الخاصة بالـ Admin
        /// </summary>
        Task<Result<bool>> UpdateLaundryProfileAsync(string adminId, SetupLaundryRequest request);

        /// <summary>
        /// بيرجع بيانات المغسلة الحالية الخاصة بالـ Admin عشان يعرضها في البروفايل
        /// </summary>
        Task<Result<LaundryProfileDto>> GetLaundryProfileAsync(string adminId);

        // ============================================================
        // COURIER ASSIGNMENT & DISPATCH APIs
        // ============================================================

        /// <summary>
        /// Assigns an order to a specific courier by phone or ID
        /// Notifications are sent to the customer that Delivery X is arriving, and their phone is disclosed
        /// </summary>
        Task<Result<bool>> AssignCourierToOrderExplicitlyAsync(string adminId, AssignCourierRequestDto request);

        /// <summary>
        /// Suggests the order to multiple available couriers within a radius. The first to click "Accept" gets it.
        /// </summary>
        Task<Result<List<AvailableCourierDto>>> SuggestOrderToAvailableCouriersAsync(string adminId, BroadcastOrderRequestDto request);

        /// <summary>
        /// بيرجع قايمة الخدمات اللي اللاندري أدمن ده ضايفها في مغسلته
        /// </summary>
        Task<Result<List<LaundryServiceDto>>> GetLaundryServicesAsync(string adminId);

        /// <summary>
        /// جلب كل الطلبات الواردة للمغسلة بتاعته بس
        /// مش بيشوف طلبات أي مغسلة تانية
        /// </summary>
        Task<Result<List<LaundryOrderDto>>> GetIncomingOrdersAsync(string adminId);

        /// <summary>
        /// جلب تفاصيل طلب معين بالـ ID
        /// بيتأكد إن الطلب ده فعلاً بتاع مغسلته
        /// </summary>
        Task<Result<LaundryOrderDto>> GetOrderByIdAsync(int orderId, string adminId);

        /// <summary>
        /// تغيير حالة الطلب (قبول / رفض / تغيير للـ InProgress أو ReadyForDelivery)
        /// </summary>
        Task<Result<bool>> UpdateOrderStatusAsync(int orderId, string adminId, UpdateOrderStatusRequest request);

        /// <summary>
        /// تغيير حالة استقبال المغسلة (Open / Closed / Busy)
        /// </summary>
        Task<Result<bool>> UpdateAvailabilityAsync(string adminId, UpdateLaundryAvailabilityRequest request);

        /// <summary>
        /// إضافة خدمة جديدة للمغسلة (غسيل، كوي، دراي كلين)
        /// </summary>
        Task<Result<bool>> AddServiceAsync(string adminId, AddServiceRequest request);

        /// <summary>
        /// تعديل بيانات أو سعر خدمة موجودة
        /// </summary>
        Task<Result<bool>> UpdateServiceAsync(int serviceId, string adminId, UpdateServiceRequest request);

        /// <summary>
        /// حذف خدمة من المغسلة
        /// </summary>
        Task<Result<bool>> DeleteServiceAsync(int serviceId, string adminId);
        
        /// <summary>
        /// بيرفع صورة المغسلة على Cloudinary
        /// وبيحفظ الـ URL في الداتا بيز
        /// </summary>
        Task<Result<string>> UploadLaundryImageAsync(string adminId, IFormFile file);
    }
}
