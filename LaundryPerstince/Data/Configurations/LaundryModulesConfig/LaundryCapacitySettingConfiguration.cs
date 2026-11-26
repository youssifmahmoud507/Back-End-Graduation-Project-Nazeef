using LaundryDomain.Models.LaundryModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.LaundryModulesConfig
{
    public class LaundryCapacitySettingConfiguration : IEntityTypeConfiguration<LaundryCapacitySetting>
    {
        public void Configure(EntityTypeBuilder<LaundryCapacitySetting> builder)
        {
            builder.ToTable("LaundryCapacitySettings");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.MaxOrdersPerDay)
                .IsRequired();

            builder.Property(c => c.LeadTimeHours)
                .IsRequired();

            builder.HasOne(c => c.Laundry)
                .WithMany()
                .HasForeignKey(c => c.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.LaundryId)
                .IsUnique();
        }
    }
}
