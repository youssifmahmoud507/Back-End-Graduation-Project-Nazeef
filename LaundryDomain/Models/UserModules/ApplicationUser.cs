using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LaundryDomain.Models.UserModules
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required(ErrorMessage = "User role is required")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Invalid user role")]
        public UserRole Role { get; set; } // Customer, Courier, LaundryAdmin, SuperAdmin

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }


        // Otp
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        // Didit Identity Verification
        public bool IsIdentityVerified { get; set; } = false;
        public string? IdCardImageUrl { get; set; }

        // Profile avatar URL (user-uploaded)
        public string? AvatarUrl { get; set; }

        // Laundry Admin Verification
        public string? CommercialRegisterDocumentUrl { get; set; }
        public LaundryAdminApprovalStatus? AdminApprovalStatus { get; set; }
        public string? AdminRejectionReason { get; set; }

        // Navigation Properties (عشان الـ EF Core)
        public ICollection<Order> CustomerOrders { get; set; } = new List<Order>();
        public ICollection<Order> CourierOrders { get; set; } = new List<Order>();
        public ICollection<Laundry> ManagedLaundries { get; set; } = new List<Laundry>();

        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public ICollection<UserPaymentMethod> PaymentMethods { get; set; } = new List<UserPaymentMethod>();
        public NotificationPreference? NotificationPreference { get; set; }
        public UserSettings? Settings { get; set; }
        public UserPoints? Points { get; set; }
    }
}
