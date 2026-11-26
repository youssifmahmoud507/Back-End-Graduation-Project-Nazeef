using LaundryDomain.Models.SystemModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryPerstince.Data.Configurations.SystemModulesConfig
{
    public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            builder.ToTable("Complaints");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Details)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(c => c.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.LaundryAdmin)
                .WithMany()
                .HasForeignKey(c => c.LaundryAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.OrderId);
            builder.HasIndex(c => c.LaundryId);
            builder.HasIndex(c => c.CustomerId);
            builder.HasIndex(c => c.LaundryAdminId);
        }
    }
}
