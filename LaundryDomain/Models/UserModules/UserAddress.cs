using LaundryDomain.Models.Common;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.UserModules
{
    public class UserAddress : BaseEntity<UserAddress, int>
    {
        public AddressType Type { get; set; } // home, work, other
        public string Label { get; set; }
        public string Street { get; set; }
        public string? Apt { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string? Instructions { get; set; }
        public bool IsDefault { get; set; } = false;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // FK
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
