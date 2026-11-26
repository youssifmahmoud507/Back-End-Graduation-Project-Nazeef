using LaundryDomain.Models.OrderModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.OrderModulesConfig
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.PickupLocation)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(o => o.DeliveryLocation)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(o => o.Status)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(o => o.TotalPrice)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            builder.Property(o => o.DiscountAmount)
                   .HasColumnType("decimal(10,2)");

            builder.Property(o => o.Notes)
                   .HasMaxLength(500);

            builder.Property(o => o.CancellationReason)
                   .HasMaxLength(500);

            // Customer Relation
            builder.HasOne(o => o.Customer)
                   .WithMany()
                   .HasForeignKey(o => o.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Courier Relation (Nullable)
            builder.HasOne(o => o.Courier)
                   .WithMany()
                   .HasForeignKey(o => o.CourierId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Laundry Relation
            builder.HasOne(o => o.Laundry)
                   .WithMany(l => l.Orders)
                   .HasForeignKey(o => o.LaundryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // OrderItems Relation
            builder.HasMany(o => o.OrderItems)
                   .WithOne(oi => oi.Order)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Payment Relation (1-to-1)
            builder.HasOne(o => o.Payment)
                   .WithOne(p => p.Order)
                   .HasForeignKey<Payment>(p => p.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(o => o.Coupon)
                   .WithMany()
                   .HasForeignKey(o => o.CouponId)
                   .OnDelete(DeleteBehavior.Restrict);

            // BundleOrderMetadata Relation (Optional)
            builder.HasOne<LaundryDomain.Models.BundleModules.BundleOrderMetadata>()
                   .WithOne(bom => bom.Order)
                   .HasForeignKey<LaundryDomain.Models.BundleModules.BundleOrderMetadata>(bom => bom.OrderId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
