using LaundryDomain.Models.BundleModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryPerstince.Data.Configurations.BundleModulesConfig
{
    public class BundleConfiguration : IEntityTypeConfiguration<Bundle>
    {
        public void Configure(EntityTypeBuilder<Bundle> builder)
        {
            builder.ToTable("Bundles");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(b => b.Description)
                   .HasMaxLength(500);

            builder.Property(b => b.BundlePrice)
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.OriginalPrice)
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.Status)
                   .HasConversion<int>();

            builder.Property(b => b.OwnerType)
                   .HasConversion<int>();

            builder.Property(b => b.ImageUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.HasQueryFilter(b => !b.IsDeleted);

            // Indexes
            builder.HasIndex(b => new { b.Status, b.ExpiryDate });
            builder.HasIndex(b => new { b.LaundryId, b.Status });
            builder.HasIndex(b => new { b.OwnerType, b.Status });

            // Admin Identity Relation (No cascade delete)
            builder.HasOne(b => b.CreatedByAdmin)
                   .WithMany()
                   .HasForeignKey(b => b.CreatedByAdminId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Laundry Relation (Nullable FK, Restrict delete)
            builder.HasOne(b => b.Laundry)
                   .WithMany(l => l.Bundles)
                   .HasForeignKey(b => b.LaundryId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // BundleItems Relation
            builder.HasMany(b => b.BundleItems)
                   .WithOne(bi => bi.Bundle)
                   .HasForeignKey(bi => bi.BundleId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ApprovalRequests Relation
            builder.HasMany(b => b.ApprovalRequests)
                   .WithOne(ar => ar.Bundle)
                   .HasForeignKey(ar => ar.BundleId)
                   .OnDelete(DeleteBehavior.Cascade);

            // BundleOrderMetadatas Relation
            builder.HasMany(b => b.BundleOrderMetadatas)
                   .WithOne(bom => bom.Bundle)
                   .HasForeignKey(bom => bom.BundleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}