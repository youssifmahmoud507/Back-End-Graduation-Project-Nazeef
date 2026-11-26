using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.Enums.CustomerEnums
{
    public enum OrderStatus
    {
        PendingConfirmation = 1,
        Accepted = 2,
        Washing = 3,
        ReadyForPickup = 4,
        PickedUp = 5,
        Delivered = 6,
        Cancelled = 7
    }
}
