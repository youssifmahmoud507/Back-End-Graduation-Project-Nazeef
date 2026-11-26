using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.Enums.CustomerEnums
{
    public enum CourierOrderFilter
    {
        All = 0,
        Pending = 1,      // AssignedToCourier
        InProgress = 2,   // PickedUp
        Completed = 3     // Delivered
    }
}
