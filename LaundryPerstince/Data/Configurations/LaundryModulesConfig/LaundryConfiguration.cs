using LaundryDomain.Models.LaundryModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.LaundryModulesConfig
{
    public class LaundryConfiguration : IEntityTypeConfiguration<Laundry>
    {
        public void Configure(EntityTypeBuilder<Laundry> builder)
        {
            builder.ToTable("Laundries");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(l => l.Address)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(l => l.Latitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(l => l.Longitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(l => l.Status)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(l => l.Availability)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(l => l.AverageRating)
                   .HasDefaultValue(0f);

            builder.Property(l => l.TotalReviews)
                   .HasDefaultValue(0);

            // Relations
            builder.HasOne(l => l.Admin)
                   .WithMany()
                   .HasForeignKey(l => l.AdminId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(l => l.Services)
                   .WithOne(s => s.Laundry)
                   .HasForeignKey(s => s.LaundryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Reviews)
                   .WithOne(r => r.Laundry)
                   .HasForeignKey(r => r.LaundryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Orders)
                   .WithOne(o => o.Laundry)
                   .HasForeignKey(o => o.LaundryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
