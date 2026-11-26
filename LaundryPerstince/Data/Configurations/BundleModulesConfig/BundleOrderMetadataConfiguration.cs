using LaundryDomain.Models.BundleModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.BundleModulesConfig
{
    public class BundleOrderMetadataConfiguration
              : IEntityTypeConfiguration<BundleOrderMetadata>
    {
        public void Configure(EntityTypeBuilder<BundleOrderMetadata> builder)
        {
            builder.ToTable("BundleOrderMetadata");

            builder.Property(m => m.BundleNameSnapshot)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.BundlePriceSnapshot)
                .HasPrecision(18, 2);

            builder.Property(m => m.OriginalPriceSnapshot)
                .HasPrecision(18, 2);

            builder.Property(m => m.SavingsAmount)
                .HasPrecision(18, 2);

            // OrderId is int — unique one-to-one with Order
            builder.HasIndex(m => m.OrderId)
                .IsUnique();

            builder.HasOne(m => m.Order)
                .WithOne()
                .HasForeignKey<BundleOrderMetadata>(m => m.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Bundle)
                .WithMany(b => b.BundleOrderMetadatas)
                .HasForeignKey(m => m.BundleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => m.BundleId);
        }
    }
}