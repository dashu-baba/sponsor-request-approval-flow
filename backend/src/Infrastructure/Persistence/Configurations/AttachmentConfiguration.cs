using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.ObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
        builder.Property(attachment => attachment.ContentType).HasMaxLength(127).IsRequired();
        builder.Property(attachment => attachment.SizeBytes).IsRequired();
        builder.Property(attachment => attachment.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(attachment => attachment.CreatedBy).HasMaxLength(450);

        builder.HasOne(attachment => attachment.SponsorshipRequest)
            .WithMany(request => request.Attachments)
            .HasForeignKey(attachment => attachment.SponsorshipRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attachment => attachment.SponsorshipRequestId);
        builder.HasIndex(attachment => attachment.ObjectKey).IsUnique();
    }
}
