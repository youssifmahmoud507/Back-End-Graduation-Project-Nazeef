using LaundryDomain.IRepositories;
using LaundryDomain.Models.SystemModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.NotificationSpecs;
using LaundryServies.Hubs;
using LaundryServiesAbstraction.Hubs;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryShared.Common;
using LaundryShared.DTO.Notifications;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.NotificationModule
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub, INotificationHub> _hubContext;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IHubContext<NotificationHub, INotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        // GET /api/notifications
        public async Task<Result<Pagination<NotificationDto>>> GetNotificationsAsync(string userId, NotificationParams notificationParams)
        {
            var spec = new NotificationsByUserSpec(userId, notificationParams);
            var countSpec = new NotificationsByUserCountSpec(userId, notificationParams);

            var notifications = await _unitOfWork.Repository<Notification, int>()
                                                  .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Notification, int>()
                                          .CountAsync(countSpec);

            var data = notifications.Select(n => MapToDto(n)).ToList();

            var pagination = new Pagination<NotificationDto>(
                notificationParams.PageIndex,
                notificationParams.PageSize,
                count,
                data
            );

            return Result<Pagination<NotificationDto>>.Success(pagination);
        }

        // GET /api/notifications/unread
        public async Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(string userId)
        {
            var spec = new UnreadNotificationsSpec(userId);

            var notifications = await _unitOfWork.Repository<Notification, int>()
                                                  .GetAllWithSpecAsync(spec);

            var data = notifications.Select(n => MapToDto(n)).ToList();

            return Result<List<NotificationDto>>.Success(data);
        }

        // GET /api/notifications/count
        public async Task<Result<int>> GetUnreadCountAsync(string userId)
        {
            var spec = new UnreadNotificationsSpec(userId);

            var count = await _unitOfWork.Repository<Notification, int>()
                                          .CountAsync(spec);

            return Result<int>.Success(count);
        }

        // PUT /api/notifications/:id/read
        public async Task<Result<bool>> MarkAsReadAsync(string userId, int notificationId)
        {
            var notification = await _unitOfWork.Repository<Notification, int>()
                                                 .GetByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
                return Result<bool>.Failure("Notification not found.");

            if (notification.IsRead)
                return Result<bool>.Success(true);

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Notification, int>().Update(notification);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // PUT /api/notifications/mark-all-read
        public async Task<Result<int>> MarkAllAsReadAsync(string userId)
        {
            var spec = new UnreadNotificationsSpec(userId);

            var notifications = await _unitOfWork.Repository<Notification, int>()
                                                  .GetAllWithSpecAsync(spec);

            var count = 0;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Notification, int>().Update(notification);
                count++;
            }

            await _unitOfWork.CompleteAsync();

            return Result<int>.Success(count);
        }

        // DELETE /api/notifications/:id
        public async Task<Result<bool>> DeleteNotificationAsync(string userId, int notificationId)
        {
            var notification = await _unitOfWork.Repository<Notification, int>()
                                                 .GetByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
                return Result<bool>.Failure("Notification not found.");

            _unitOfWork.Repository<Notification, int>().Delete(notification);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // DELETE /api/notifications/clear-all
        public async Task<Result<int>> ClearAllReadAsync(string userId)
        {
            var spec = new ReadNotificationsSpec(userId);

            var notifications = await _unitOfWork.Repository<Notification, int>()
                                                  .GetAllWithSpecAsync(spec);

            var count = 0;
            foreach (var notification in notifications)
            {
                _unitOfWork.Repository<Notification, int>().Delete(notification);
                count++;
            }

            await _unitOfWork.CompleteAsync();

            return Result<int>.Success(count);
        }

        // GET /api/notifications/preferences
        public async Task<Result<NotificationPreferenceDto>> GetPreferencesAsync(string userId)
        {
            var spec = new NotificationPreferenceByUserSpec(userId);

            var preference = await _unitOfWork.Repository<NotificationPreference, int>()
                                               .GetEntityWithSpecAsync(spec);

            // لو مش موجودة نرجع Default
            if (preference == null)
                return Result<NotificationPreferenceDto>.Success(new NotificationPreferenceDto
                {
                    WhatsappEnabled = true,
                    SmsEnabled = true,
                    EmailEnabled = true,
                    PushEnabled = true,
                    OrderUpdates = true,
                    Promotions = true,
                    Newsletter = false
                });

            return Result<NotificationPreferenceDto>.Success(new NotificationPreferenceDto
            {
                WhatsappEnabled = preference.WhatsappEnabled,
                SmsEnabled = preference.SmsEnabled,
                EmailEnabled = preference.EmailEnabled,
                PushEnabled = preference.PushEnabled,
                OrderUpdates = preference.OrderUpdates,
                Promotions = preference.Promotions,
                Newsletter = preference.Newsletter
            });
        }

        // PUT /api/notifications/preferences
        public async Task<Result<NotificationPreferenceDto>> UpdatePreferencesAsync(string userId, UpdateNotificationPreferenceRequest request)
        {
            var spec = new NotificationPreferenceByUserSpec(userId);

            var preference = await _unitOfWork.Repository<NotificationPreference, int>()
                                               .GetEntityWithSpecAsync(spec);

            // لو مش موجودة نعملها
            if (preference == null)
            {
                preference = new NotificationPreference
                {
                    UserId = userId,
                    WhatsappEnabled = request.WhatsappEnabled,
                    SmsEnabled = request.SmsEnabled,
                    EmailEnabled = request.EmailEnabled,
                    PushEnabled = request.PushEnabled,
                    OrderUpdates = request.OrderUpdates,
                    Promotions = request.Promotions,
                    Newsletter = request.Newsletter
                };

                await _unitOfWork.Repository<NotificationPreference, int>().AddAsync(preference);
            }
            else
            {
                preference.WhatsappEnabled = request.WhatsappEnabled;
                preference.SmsEnabled = request.SmsEnabled;
                preference.EmailEnabled = request.EmailEnabled;
                preference.PushEnabled = request.PushEnabled;
                preference.OrderUpdates = request.OrderUpdates;
                preference.Promotions = request.Promotions;
                preference.Newsletter = request.Newsletter;
                preference.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<NotificationPreference, int>().Update(preference);
            }

            await _unitOfWork.CompleteAsync();

            return Result<NotificationPreferenceDto>.Success(new NotificationPreferenceDto
            {
                WhatsappEnabled = preference.WhatsappEnabled,
                SmsEnabled = preference.SmsEnabled,
                EmailEnabled = preference.EmailEnabled,
                PushEnabled = preference.PushEnabled,
                OrderUpdates = preference.OrderUpdates,
                Promotions = preference.Promotions,
                Newsletter = preference.Newsletter
            });
        }

        // ============================================================
        // SignalR - بعت الإشعارات في real-time
        // ============================================================

        public async Task SendNotificationAsync(string userId, string title, string message)
        {
            var type = LaundryShared.Enums.CustomerEnums.NotificationType.Promotion; // Default/System type
            
            string lowerTitle = title?.ToLower() ?? "";
            string lowerMsg = message?.ToLower() ?? "";
            
            if (lowerTitle.Contains("order") || lowerTitle.Contains("طلب") || 
                lowerTitle.Contains("courier") || lowerTitle.Contains("laundry") || 
                lowerTitle.Contains("delivery") || lowerTitle.Contains("pickup") ||
                lowerTitle.Contains("مندوب") || lowerTitle.Contains("مغسلة") ||
                lowerMsg.Contains("order") || lowerMsg.Contains("طلب") || 
                lowerMsg.Contains("courier") || lowerMsg.Contains("laundry") || 
                lowerMsg.Contains("delivery") || lowerMsg.Contains("pickup") ||
                lowerMsg.Contains("مندوب") || lowerMsg.Contains("مغسلة"))
            {
                type = LaundryShared.Enums.CustomerEnums.NotificationType.OrderCreated;
            }
            else if (lowerTitle.Contains("payment") || lowerTitle.Contains("charge") || 
                     lowerTitle.Contains("pay") || lowerTitle.Contains("دفع") || 
                     lowerTitle.Contains("شحن") ||
                     lowerMsg.Contains("payment") || lowerMsg.Contains("charge") || 
                     lowerMsg.Contains("pay") || lowerMsg.Contains("دفع") || 
                     lowerMsg.Contains("شحن"))
            {
                type = LaundryShared.Enums.CustomerEnums.NotificationType.PaymentSuccess;
            }

            // بنحفظ الإشعار في الداتا بيز
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification, int>().AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            // بنبعته في real-time باستخدام Strongly Typed Client
            await _hubContext.Clients.Group(userId).ReceiveNotification(new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = (int)notification.Type,
                IsRead = notification.IsRead,
                notification.CreatedAt
            });
        }

        public async Task SendOrderConfirmedAsync(string customerId, int orderId)
        {
            await SendNotificationAsync(
                customerId,
                "Order Confirmed ✅",
                $"Your order #{orderId} has been confirmed by the laundry"
            );
        }

        public async Task SendOrderInProgressAsync(string customerId, int orderId)
        {
            await SendNotificationAsync(
                customerId,
                "Order In Progress 🔄",
                $"Your order #{orderId} is currently being washed"
            );
        }

        public async Task SendOrderReadyAsync(string customerId, int orderId)
        {
            await SendNotificationAsync(
                customerId,
                "Order Ready 📦",
                $"Your order #{orderId} is ready for delivery"
            );
        }

        public async Task SendOrderDeliveredAsync(string customerId, int orderId)
        {
            await SendNotificationAsync(
                customerId,
                "Order Delivered 🎉",
                $"Your order #{orderId} has been delivered successfully"
            );
        }

        public async Task SendOrderCancelledAsync(string customerId, int orderId, string reason)
        {
            await SendNotificationAsync(
                customerId,
                "Order Cancelled ❌",
                $"Your order #{orderId} has been cancelled. Reason: {reason}"
            );
        }

        // ============================================================
        // ORDER ASSIGNMENT & DISPATCH (Broadcast to Couriers)
        // ============================================================

        public async Task SendOrderBroadcastToCouriersAsync(List<string> courierUserIds, int orderId, string laundryName, string pickupArea, string dropArea)
        {
            foreach (var courierId in courierUserIds)
            {
                await SendNotificationAsync(
                    courierId,
                    "New Order Available! 🚀",
                    $"Laundry {laundryName} has an order from {pickupArea} to {dropArea}. Tap to accept first!"
                );
            }
        }

        public async Task SendCourierAssignedToCustomerAsync(string customerId, int orderId, string courierName, string courierPhone, string eta)
        {
            await SendNotificationAsync(
                customerId,
                "Courier Assigned 🚲",
                $"Courier {courierName} ({courierPhone}) is picking up your order #{orderId}. ETA: {eta}"
            );
        }

        // Private Mapping
        private NotificationDto MapToDto(Notification n) => new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
