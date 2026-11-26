using LaundryDomain.Models.BundleModules;
using LaundryDomain.Models.CourierModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.SystemModules;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Models.Wallet;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Context
{
    public class NadeefDbContext : IdentityDbContext<ApplicationUser>
    {
        public NadeefDbContext(DbContextOptions<NadeefDbContext> options) : base(options)
        {
        }

        // ── Laundry Modules ──────────────────────────
        public DbSet<Laundry> Laundries { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<LaundryAvailabilitySchedule> LaundryAvailabilitySchedules { get; set; }
        public DbSet<LaundryCapacitySetting> LaundryCapacitySettings { get; set; }
        public DbSet<LaundryClosedDate> LaundryClosedDates { get; set; }

        // ── Order Modules ────────────────────────────
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Coupon> Coupons { get; set; }

        // ── System Modules ───────────────────────────
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Complaint> Complaints { get; set; }

        // ── Bundle Modules ───────────────────────────
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<BundleItem> BundleItems { get; set; }
        public DbSet<BundleApprovalRequest> BundleApprovalRequests { get; set; }
        public DbSet<BundleOrderMetadata> BundleOrderMetadatas { get; set; }


        // ── Wallet Modules (Week 8 basel ) ───────────────────

        public DbSet<CustomerWallet> CustomerWallets { get; set; }
        public DbSet<LaundryWallet> LaundryWallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<LaundryDomain.Models.Wallet.CommissionLog> CommissionLogs { get; set; }
        public DbSet<LaundryDomain.Models.Wallet.CommissionPayment> CommissionPayments { get; set; }
        public DbSet<LaundryPayoutProfile> LaundryPayoutProfiles { get; set; }
        public DbSet<LaundryPayout> LaundryPayouts { get; set; }

        // ── Fraud Modules ───────────────────
        public DbSet<SuspiciousActivity> SuspiciousActivities { get; set; }

        // ── User Modules (Week 4) ────────────────────
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<UserPoints> UserPoints { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }

        // ── Courier Modules (Week 5) ─────────────────
        public DbSet<CourierProfile> CourierProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
