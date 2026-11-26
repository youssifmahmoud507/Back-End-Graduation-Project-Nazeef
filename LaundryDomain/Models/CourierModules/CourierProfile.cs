using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.UserModules;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryDomain.Models.CourierModules
{
    public class CourierProfile : BaseEntity<CourierProfile, int>
    {
        [Required]
        public string CourierId { get; set; }

        public int? LaundryId { get; set; }

        public int CompletedOrdersCount { get; set; } = 0;

        public int CancellationCount { get; set; } = 0;

        public double? CurrentLatitude { get; set; }

        public double? CurrentLongitude { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation
        public ApplicationUser Courier { get; set; }
        public Laundry? Laundry { get; set; }
    }
}
