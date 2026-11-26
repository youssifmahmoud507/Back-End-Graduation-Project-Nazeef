using LaundryDomain.Models.BundleModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.BundleModulesConfig
{
    public class BundleItemConfiguration : IEntityTypeConfiguration<BundleItem>
    {
        public void Configure(EntityTypeBuilder<BundleItem> builder)
        {
            builder.ToTable("BundleItems");

            builder.HasKey(bi => bi.Id);

            builder.Property(bi => bi.AllowedItemCategory)
                   .HasConversion<int?>();

            // Relationships defined in Bundle configuration (Cascade)
            // Implicit from Bundle to BundleItem

            // Service Relation
            builder.HasOne(bi => bi.Service)
                   .WithMany() // Assuming no BundleItems collection in Service based on provided constraints
                   .HasForeignKey(bi => bi.ServiceId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}