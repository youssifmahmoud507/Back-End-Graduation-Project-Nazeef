using LaundryDomain.Models.LaundryModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.LaundryModulesConfig
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.ServiceName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.Category)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(s => s.Price)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            builder.Property(s => s.IsAvailable)
                   .HasDefaultValue(true);

            builder.Property(s => s.Category)
                    .HasConversion<int>();
        }
    }
}
