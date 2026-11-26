using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Language)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(s => s.Currency)
                   .HasConversion<int>()
                   .IsRequired();

            // One to One
            builder.HasOne(s => s.User)
                   .WithOne(u => u.Settings)
                   .HasForeignKey<UserSettings>(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
