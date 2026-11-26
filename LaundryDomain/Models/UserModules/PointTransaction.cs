using LaundryDomain.Models.Common;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Models.UserModules
{
    public class PointTransaction : BaseEntity<PointTransaction, int>
    {
        public int Points { get; set; }
        public PointTransactionType Type { get; set; }
        public string Description { get; set; }
        public int? OrderId { get; set; }

        // FK
        public int UserPointsId { get; set; }
        public UserPoints UserPoints { get; set; }

    }
}
