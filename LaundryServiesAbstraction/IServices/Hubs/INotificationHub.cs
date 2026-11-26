using System.Threading.Tasks;

namespace LaundryServiesAbstraction.Hubs
{
    public interface INotificationHub
    {
        Task ReceiveNotification(object notification);
    }
}