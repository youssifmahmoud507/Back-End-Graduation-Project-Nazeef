using LaundryDomain.Models.BundleModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.BundleModulesConfig
{
    public class BundleApprovalRequestConfiguration : IEntityTypeConfiguration<BundleApprovalRequest>
    {
        public void Configure(EntityTypeBuilder<BundleApprovalRequest> builder)
        {
            builder.ToTable("BundleApprovalRequests");

            builder.HasKey(ar => ar.Id);

            builder.Property(ar => ar.RequestNote)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(ar => ar.ReviewNote)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(ar => ar.PendingBundleSnapshot)
                   .HasMaxLength(4000)
                   .IsRequired(false);

            builder.Property(ar => ar.Status)
                   .HasConversion<int>();

            // Indexes
            builder.HasIndex(ar => new { ar.BundleId, ar.Status });
            builder.HasIndex(ar => ar.Status);

            // Relationships
            // Bundle Relation is Cascade and driven by Bundle config

            builder.HasOne(ar => ar.RequestedByAdmin)
                   .WithMany()
                   .HasForeignKey(ar => ar.RequestedByAdminId)
                   .HasConstraintName("FK_BundleApprovalRequests_AspNetUsers_RequestedByAdminId")
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.ReviewedByAdmin)
                   .WithMany()
                   .HasForeignKey(ar => ar.ReviewedByAdminId)
                   .IsRequired(false)
                   .HasConstraintName("FK_BundleApprovalRequests_AspNetUsers_ReviewedByAdminId")
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}