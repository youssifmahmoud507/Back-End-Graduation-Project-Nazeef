using LaundryServiesAbstraction.IServices.Notifications;
using LaundryShared.DTO.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaundryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("uid")
            ?? string.Empty;

        // GET /api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] NotificationParams notificationParams)
        {
            var result = await _notificationService.GetNotificationsAsync(GetUserId(), notificationParams);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/notifications/unread
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var result = await _notificationService.GetUnreadNotificationsAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // GET /api/notifications/count
        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var result = await _notificationService.GetUnreadCountAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { UnreadCount = result.Value });
        }

        // GET /api/notifications/preferences
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            var result = await _notificationService.GetPreferencesAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/notifications/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _notificationService.MarkAllAsReadAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { UpdatedCount = result.Value });
        }

        // PUT /api/notifications/preferences
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferenceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _notificationService.UpdatePreferencesAsync(GetUserId(), request);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Value);
        }

        // PUT /api/notifications/:id/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Notification marked as read." });
        }

        // DELETE /api/notifications/clear-all
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllRead()
        {
            var result = await _notificationService.ClearAllReadAsync(GetUserId());

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { DeletedCount = result.Value });
        }

        // DELETE /api/notifications/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var result = await _notificationService.DeleteNotificationAsync(GetUserId(), id);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Notification deleted successfully." });
        }
    }
}
