using LaundryDomain.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.LaundryModules
{
    public class LaundryAvailabilitySchedule : BaseEntity<LaundryAvailabilitySchedule, int>
    {
        [Required]
        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; } = null!;

        [Required]
        public int DayOfWeek { get; set; } // 0=Sunday..6=Saturday

        [Required]
        public bool IsOpen { get; set; } = true;

        [Required]
        public TimeSpan OpenTime { get; set; }

        [Required]
        public TimeSpan CloseTime { get; set; }
    }
}
