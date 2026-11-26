using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
    {
        public void Configure(EntityTypeBuilder<UserAddress> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(a => a.Label)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.Street)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(a => a.Apt)
                   .HasMaxLength(50);

            builder.Property(a => a.City)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.Area)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.Instructions)
                   .HasMaxLength(500);

            builder.Property(a => a.Latitude)
                   .HasColumnType("decimal(9,6)");

            builder.Property(a => a.Longitude)
                   .HasColumnType("decimal(9,6)");

            builder.HasOne(a => a.User)
                   .WithMany(u => u.Addresses)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
