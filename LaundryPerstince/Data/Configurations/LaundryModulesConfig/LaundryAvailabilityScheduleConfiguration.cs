using LaundryDomain.Models.LaundryModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.LaundryModulesConfig
{
    public class LaundryAvailabilityScheduleConfiguration : IEntityTypeConfiguration<LaundryAvailabilitySchedule>
    {
        public void Configure(EntityTypeBuilder<LaundryAvailabilitySchedule> builder)
        {
            builder.ToTable("LaundryAvailabilitySchedules");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.DayOfWeek)
                .IsRequired();

            builder.Property(s => s.IsOpen)
                .HasDefaultValue(true);

            builder.Property(s => s.OpenTime)
                .IsRequired();

            builder.Property(s => s.CloseTime)
                .IsRequired();

            builder.HasOne(s => s.Laundry)
                .WithMany()
                .HasForeignKey(s => s.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.LaundryId, s.DayOfWeek })
                .IsUnique();
        }
    }
}
