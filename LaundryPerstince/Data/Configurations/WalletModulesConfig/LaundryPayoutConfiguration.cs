using LaundryDomain.Models.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.WalletModulesConfig
{
    public class LaundryPayoutConfiguration : IEntityTypeConfiguration<LaundryPayout>
    {
        public void Configure(EntityTypeBuilder<LaundryPayout> builder)
        {
            builder.ToTable("LaundryPayouts");

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Completed");

            builder.Property(p => p.Method)
                .HasMaxLength(50);

            builder.Property(p => p.Reference)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(p => p.Notes)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(p => p.CreatedBy)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.HasOne(p => p.Laundry)
                .WithMany()
                .HasForeignKey(p => p.LaundryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
