using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
    {
        public void Configure(EntityTypeBuilder<UserFavorite> builder)
        {
            builder.HasKey(f => f.Id);

            // Unique constraint - مش ينفع تضيف نفس المغسلة مرتين
            builder.HasIndex(f => new { f.UserId, f.LaundryId })
                   .IsUnique();

            builder.HasOne(f => f.User)
                   .WithMany(u => u.Favorites)
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Laundry)
                   .WithMany()
                   .HasForeignKey(f => f.LaundryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
