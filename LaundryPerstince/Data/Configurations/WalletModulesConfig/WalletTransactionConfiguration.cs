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
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");

            builder.Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.CommissionRate)
                .HasColumnType("decimal(5,2)");

            builder.Property(t => t.CommissionAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Type)
                .HasConversion<int>();

            builder.Property(t => t.Source)
                .HasConversion<int>();

            builder.Property(t => t.Status)
                .HasConversion<int>()
                .HasDefaultValue(TransactionStatus.Pending);

            builder.Property(t => t.PaymobTransactionId)
                .HasMaxLength(100)
                .IsRequired(false);

            // علاقة مع محفظة المغسلة
            builder.HasOne(t => t.LaundryWallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.LaundryWalletId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // علاقة مع محفظة العميل
            builder.HasOne(t => t.CustomerWallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.CustomerWalletId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // علاقة مع الأوردر
            builder.HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
