using LaundryDomain.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.LaundryModules
{
    public class LaundryCapacitySetting : BaseEntity<LaundryCapacitySetting, int>
    {
        [Required]
        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; } = null!;

        [Required]
        public int MaxOrdersPerDay { get; set; } = 30;

        [Required]
        public int LeadTimeHours { get; set; } = 2;
    }
}
