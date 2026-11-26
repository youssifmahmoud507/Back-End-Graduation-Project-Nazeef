using LaundryDomain.Models.LaundryModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.LaundryModulesConfig
{
    public class LaundryClosedDateConfiguration : IEntityTypeConfiguration<LaundryClosedDate>
    {
        public void Configure(EntityTypeBuilder<LaundryClosedDate> builder)
        {
            builder.ToTable("LaundryClosedDates");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Date)
                .IsRequired();

            builder.Property(c => c.Reason)
                .HasMaxLength(200);

            builder.HasOne(c => c.Laundry)
                .WithMany()
                .HasForeignKey(c => c.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => new { c.LaundryId, c.Date });
        }
    }
}
