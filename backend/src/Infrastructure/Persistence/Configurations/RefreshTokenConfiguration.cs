using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId).HasMaxLength(450).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(token => token.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.RevokedAt });
    }
}
