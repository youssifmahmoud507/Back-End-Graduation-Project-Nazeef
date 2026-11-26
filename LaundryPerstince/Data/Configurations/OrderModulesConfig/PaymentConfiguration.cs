using LaundryDomain.Models.OrderModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.OrderModulesConfig
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            builder.Property(p => p.PaymentStatus)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(p => p.PaymentMethod)
                   .IsRequired()
                   .HasConversion<int>();
        }
    }
}
