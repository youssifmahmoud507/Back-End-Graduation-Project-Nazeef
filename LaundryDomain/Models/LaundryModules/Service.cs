using LaundryDomain.Models.Common;
using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.LaundryModules
{
    public class Service : BaseEntity<Service, int>
    {
        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; }

        [Required]
        [MaxLength(50)]
        public ServiceCategory Category { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        // FK
        [Required]
        public int LaundryId { get; set; }

        // Navigation Properties
        public Laundry Laundry { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
