using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class UserPointsConfiguration : IEntityTypeConfiguration<UserPoints>
    {
        public void Configure(EntityTypeBuilder<UserPoints> builder)
        {
            builder.HasKey(p => p.Id);

            // One to One
            builder.HasOne(p => p.User)
                   .WithOne(u => u.Points)
                   .HasForeignKey<UserPoints>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // One to Many مع Transactions
            builder.HasMany(p => p.Transactions)
                   .WithOne(t => t.UserPoints)
                   .HasForeignKey(t => t.UserPointsId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
