using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
    {
        public void Configure(EntityTypeBuilder<PointTransaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(t => t.Description)
                   .IsRequired()
                   .HasMaxLength(500);
        }
    }
}
