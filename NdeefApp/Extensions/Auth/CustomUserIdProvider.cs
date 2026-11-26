using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LaundryApi.Extensions.Auth
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? connection.User?.FindFirst("uid")?.Value;
        }
    }
}
