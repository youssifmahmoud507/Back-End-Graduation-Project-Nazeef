using LaundryDomain.Models.CourierModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations
{
    public class CourierProfileConfiguration : IEntityTypeConfiguration<CourierProfile>
    {
        public void Configure(EntityTypeBuilder<CourierProfile> builder)
        {
            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.CourierId)
                .IsRequired();

            builder.Property(cp => cp.LaundryId)
                .IsRequired(false);

            builder.HasIndex(cp => cp.CourierId)
                .IsUnique(); // كل courier ليه profile واحد

            builder.Property(cp => cp.CompletedOrdersCount)
                .HasDefaultValue(0);

            builder.Property(cp => cp.CancellationCount)
                .HasDefaultValue(0);

            builder.Property(cp => cp.IsAvailable)
                .HasDefaultValue(true);

            builder.HasOne(cp => cp.Courier)
                .WithOne()
                .HasForeignKey<CourierProfile>(cp => cp.CourierId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.Laundry)
                .WithMany(l => l.Couriers)
                .HasForeignKey(cp => cp.LaundryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
