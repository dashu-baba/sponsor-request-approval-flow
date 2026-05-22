using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class SponsorshipTypeConfiguration : IEntityTypeConfiguration<SponsorshipType>
{
    public void Configure(EntityTypeBuilder<SponsorshipType> builder)
    {
        builder.ToTable("sponsorship_types");

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Name).HasMaxLength(120).IsRequired();
        builder.Property(type => type.Description).HasMaxLength(1000);
        builder.Property(type => type.IsActive).IsRequired();
        builder.Property(type => type.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(type => type.CreatedBy).HasMaxLength(450);
        builder.Property(type => type.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(type => type.UpdatedBy).HasMaxLength(450);

        builder.HasIndex(type => type.Name)
            .IsUnique()
            .HasFilter("is_active = true");
        builder.HasIndex(type => type.IsActive);
    }
}
