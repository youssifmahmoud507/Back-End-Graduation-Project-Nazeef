using LaundryDomain.Models.Common;

namespace LaundryDomain.Models.UserModules
{
    public class UserPoints : BaseEntity<UserPoints, int>
    {
        public int TotalPoints { get; set; } = 0;
        public List<PointTransaction> Transactions { get; set; } = new List<PointTransaction>();

        // FK - One to One
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
