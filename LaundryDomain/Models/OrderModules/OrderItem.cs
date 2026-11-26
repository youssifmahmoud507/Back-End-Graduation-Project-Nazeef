using LaundryDomain.Models.Common;
using LaundryDomain.Models.LaundryModules;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.OrderModules
{
    public class OrderItem : BaseEntity<OrderItem, int>
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        // FKs
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public Guid? BundleItemId { get; set; }

        // Navigation Properties
        public Order Order { get; set; }
        public Service Service { get; set; }
    }
}
