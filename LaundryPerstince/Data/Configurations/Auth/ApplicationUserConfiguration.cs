using LaundryDomain.Models.UserModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.Configurations.Auth
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // 1. تغيير اسم الجدول في الداتا بيز بدل الاسم الافتراضي (AspNetUsers)
            builder.ToTable("Users");

            // 2. إعدادات الخصائص (Properties)
            builder.Property(u => u.Name)
                   .HasMaxLength(100);

            builder.Property(u => u.Role)
                   .IsRequired()
                   .HasConversion<int>(); // بنحفظ الـ Enum كـ رقم في الداتا بيز عشان الـ Performance

            builder.Property(u => u.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()"); // بياخد الوقت الحالي من السيرفر أوتوماتيك

            // 3. لو في علاقات (Relations) تخص اليوزر بنكتبها هنا لاحقاً
        }
    }
}
