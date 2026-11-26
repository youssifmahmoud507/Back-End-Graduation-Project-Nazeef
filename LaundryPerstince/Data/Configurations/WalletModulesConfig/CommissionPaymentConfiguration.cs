using LaundryDomain.Models.Wallet;
using LaundryShared.Enums.WalletEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.WalletModulesConfig
{
    public class CommissionPaymentConfiguration : IEntityTypeConfiguration<CommissionPayment>
    {
        public void Configure(EntityTypeBuilder<CommissionPayment> builder)
        {
            builder.ToTable("CommissionPayments");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(c => c.Currency)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(c => c.Provider)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.MerchantOrderId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Status)
                .HasConversion<int>()
                .HasDefaultValue(CommissionPaymentStatus.Unknown);

            builder.HasOne(c => c.Laundry)
                .WithMany()
                .HasForeignKey(c => c.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.LaundryAdmin)
                .WithMany()
                .HasForeignKey(c => c.LaundryAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.LaundryId);
            builder.HasIndex(c => c.LaundryAdminId);
            builder.HasIndex(c => c.MerchantOrderId)
                .IsUnique();
        }
    }
}
