using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class UserPaymentMethodConfiguration : IEntityTypeConfiguration<UserPaymentMethod>
    {
        public void Configure(EntityTypeBuilder<UserPaymentMethod> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(p => p.CardBrand)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(p => p.CardLast4)
                   .IsRequired()
                   .HasMaxLength(4);

            builder.Property(p => p.CardholderName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(p => p.ExpiryMonth)
                   .IsRequired()
                   .HasMaxLength(2);

            builder.Property(p => p.ExpiryYear)
                   .IsRequired()
                   .HasMaxLength(4);

            builder.HasOne(p => p.User)
                   .WithMany(u => u.PaymentMethods)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
