using LaundryDomain.Models.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.WalletModulesConfig
{
    public class CustomerWalletConfiguration : IEntityTypeConfiguration<CustomerWallet>
    {
        public void Configure(EntityTypeBuilder<CustomerWallet> builder)
        {
            builder.ToTable("CustomerWallets");

            builder.Property(w => w.Balance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(w => w.TotalCharged)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(w => w.IsActive)
                .HasDefaultValue(true);

            // علاقة مع العميل
            builder.HasOne(w => w.Customer)
                .WithOne()
                .HasForeignKey<CustomerWallet>(w => w.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
