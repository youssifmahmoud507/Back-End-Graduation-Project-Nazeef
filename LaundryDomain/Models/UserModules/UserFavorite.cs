using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;

namespace LaundryDomain.Models.UserModules
{
    public class UserFavorite : BaseEntity<UserFavorite, int>
    {
        // FK
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; }
    }
}
