using LaundryDomain.Models.Common;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.SystemModules;
using LaundryDomain.Models.UserModules;
using LaundryShared.Enums.CustomerEnums;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.LaundryModules
{
    public class Laundry : BaseEntity<Laundry, int>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Address { get; set; }

        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }

        [Required]
        [MaxLength(20)]
        public LaundryStatus Status { get; set; } = LaundryStatus.Active;

        [Required]
        [MaxLength(20)]
        public LaundryAvailability Availability { get; set; } = LaundryAvailability.Open;

        public float AverageRating { get; set; } = 0;

        public int TotalReviews { get; set; } = 0;

        // الصورة الخاصة بالمغسلة
        public string? ImageUrl { get; set; }

        // FK
        [Required]
        public string AdminId { get; set; }

        // Navigation Properties
        public ApplicationUser Admin { get; set; }
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<LaundryDomain.Models.CourierModules.CourierProfile> Couriers { get; set; } = new List<LaundryDomain.Models.CourierModules.CourierProfile>();
        public ICollection<LaundryDomain.Models.BundleModules.Bundle> Bundles { get; set; } = new List<LaundryDomain.Models.BundleModules.Bundle>();
    }
}
