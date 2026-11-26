using LaundryDomain.Models.Wallet;
using LaundryShared.Enums.WalletEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.WalletModulesConfig
{
    public class LaundryPayoutProfileConfiguration : IEntityTypeConfiguration<LaundryPayoutProfile>
    {
        public void Configure(EntityTypeBuilder<LaundryPayoutProfile> builder)
        {
            builder.ToTable("LaundryPayoutProfiles");

            builder.Property(p => p.TransferMethod)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.TransferType)
                .HasConversion<int?>();

            builder.Property(p => p.RecipientFullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(p => p.RecipientMobileNumber)
                .HasMaxLength(30);

            builder.Property(p => p.BankName)
                .HasMaxLength(100);

            builder.Property(p => p.BankAccountNumber)
                .HasMaxLength(34);

            builder.Property(p => p.CardNumber)
                .HasMaxLength(25);

            builder.Property(p => p.NationalId)
                .HasMaxLength(20);

            builder.HasOne(p => p.Laundry)
                .WithMany()
                .HasForeignKey(p => p.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
