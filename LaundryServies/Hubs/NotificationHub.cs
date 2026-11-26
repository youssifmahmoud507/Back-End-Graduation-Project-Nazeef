using LaundryServiesAbstraction.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LaundryServies.Hubs
{
    /// <summary>
    /// ده الـ Hub بتاع الـ SignalR
    /// اتنقل هنا عشان LaundryServies يقدر يستخدمه
    /// من غير Circular Reference
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub<INotificationHub>
    {
        /// <summary>
        /// بيتنادى تلقائي لما اليوزر يتكونكت
        /// بيضيفه في Group خاص بالـ ID بتاعه
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// بيتنادى تلقائي لما اليوزر يتديسكونكت
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}