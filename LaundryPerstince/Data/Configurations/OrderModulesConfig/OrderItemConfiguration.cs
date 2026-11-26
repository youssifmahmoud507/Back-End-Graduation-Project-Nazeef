using LaundryDomain.Models.OrderModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.OrderModulesConfig
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(oi => oi.Id);

            builder.Property(oi => oi.Quantity)
                   .IsRequired();

            builder.Property(oi => oi.UnitPrice)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            builder.Property(oi => oi.Subtotal)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            // Service Relation
            builder.HasOne(oi => oi.Service)
                   .WithMany(s => s.OrderItems)
                   .HasForeignKey(oi => oi.ServiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            // BundleItem Relation (Optional)
            builder.HasOne<LaundryDomain.Models.BundleModules.BundleItem>()
                   .WithMany()
                   .HasForeignKey(oi => oi.BundleItemId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
