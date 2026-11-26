using LaundryDomain.Models.Wallet;
using LaundryShared.Enums.WalletEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.WalletModulesConfig
{
    public class LaundryWalletConfiguration : IEntityTypeConfiguration<LaundryWallet>
    {
        public void Configure(EntityTypeBuilder<LaundryWallet> builder)
        {
            builder.ToTable("LaundryWallets");

            builder.Property(w => w.TotalEarnings)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(w => w.AvailableBalance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(w => w.PendingCommission)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(w => w.DebtCeiling)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(200);

            builder.Property(w => w.Status)
     .HasConversion<int>()
     .HasDefaultValue(LaundryWalletStatus.Active)
     .HasSentinel(LaundryWalletStatus.Active);

            // علاقة مع المغسلة
            builder.HasOne(w => w.Laundry)
                .WithOne()
                .HasForeignKey<LaundryWallet>(w => w.LaundryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
