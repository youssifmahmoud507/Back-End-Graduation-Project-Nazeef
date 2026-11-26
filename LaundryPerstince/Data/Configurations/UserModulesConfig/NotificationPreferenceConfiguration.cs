using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.UserModulesConfig
{
    public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.HasKey(n => n.Id);

            // One to One
            builder.HasOne(n => n.User)
                   .WithOne(u => u.NotificationPreference)
                   .HasForeignKey<NotificationPreference>(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
