using LaundryShared.Common;
using LaundryShared.DTO.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServiesAbstraction.IServices.Notifications
{
    /// <summary>
    /// Interface موحد بيعمل حاجتين:
    /// 1. إدارة الإشعارات في الداتا بيز (CRUD)
    /// 2. بعت الإشعارات في real-time عن طريق SignalR
    /// </summary>
    public interface INotificationService
    {
        // ============================================================
        // CRUD - إدارة الإشعارات (Team 1)
        // ============================================================

        /// <summary>جيب كل إشعارات اليوزر مع Pagination</summary>
        Task<Result<Pagination<NotificationDto>>> GetNotificationsAsync(
            string userId, NotificationParams notificationParams);

        /// <summary>جيب الإشعارات اللي لسه متقراتش</summary>
        Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(string userId);

        /// <summary>جيب عدد الإشعارات الغير مقروءة</summary>
        Task<Result<int>> GetUnreadCountAsync(string userId);

        /// <summary>علم إشعار معين كمقروء</summary>
        Task<Result<bool>> MarkAsReadAsync(string userId, int notificationId);

        /// <summary>علم كل الإشعارات كمقروءة</summary>
        Task<Result<int>> MarkAllAsReadAsync(string userId);

        /// <summary>امسح إشعار معين</summary>
        Task<Result<bool>> DeleteNotificationAsync(string userId, int notificationId);

        /// <summary>امسح كل الإشعارات المقروءة</summary>
        Task<Result<int>> ClearAllReadAsync(string userId);

        /// <summary>جيب إعدادات الإشعارات</summary>
        Task<Result<NotificationPreferenceDto>> GetPreferencesAsync(string userId);

        /// <summary>عدل إعدادات الإشعارات</summary>
        Task<Result<NotificationPreferenceDto>> UpdatePreferencesAsync(
            string userId, UpdateNotificationPreferenceRequest request);

        // ============================================================
        // SignalR - بعت الإشعارات في real-time (Dev 2)
        // ============================================================

        /// <summary>بعت إشعار عام ليوزر معين</summary>
        Task SendNotificationAsync(string userId, string title, string message);

        /// <summary>لما صاحب المغسلة يقبل الطلب</summary>
        Task SendOrderConfirmedAsync(string customerId, int orderId);

        /// <summary>لما الغسيل يبدأ</summary>
        Task SendOrderInProgressAsync(string customerId, int orderId);

        /// <summary>لما الطلب يبقى جاهز للتوصيل</summary>
        Task SendOrderReadyAsync(string customerId, int orderId);

        /// <summary>لما الطلب يتوصل</summary>
        Task SendOrderDeliveredAsync(string customerId, int orderId);

        /// <summary>لما الطلب يتلغي</summary>
        Task SendOrderCancelledAsync(string customerId, int orderId, string reason);

        // ============================================================
        // ORDER ASSIGNMENT & DISPATCH (Broadcast to Couriers)
        // ============================================================

        /// <summary>
        /// Broadcasts an order suggestion to a list of available couriers in the radius.
        /// </summary>
        Task SendOrderBroadcastToCouriersAsync(List<string> courierUserIds, int orderId, string laundryName, string pickupArea, string dropArea);

        /// <summary>
        /// Send direct notification to customer that a specific courier is arriving with their phone number.
        /// </summary>
        Task SendCourierAssignedToCustomerAsync(string customerId, int orderId, string courierName, string courierPhone, string eta);
    }
}
