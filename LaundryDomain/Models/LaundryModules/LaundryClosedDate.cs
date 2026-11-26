using LaundryDomain.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.LaundryModules
{
    public class LaundryClosedDate : BaseEntity<LaundryClosedDate, int>
    {
        [Required]
        public int LaundryId { get; set; }
        public Laundry Laundry { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; }
    }
}
