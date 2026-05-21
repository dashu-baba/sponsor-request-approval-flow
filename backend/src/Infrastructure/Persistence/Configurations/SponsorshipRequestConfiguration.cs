using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class SponsorshipRequestConfiguration : IEntityTypeConfiguration<SponsorshipRequest>
{
    public void Configure(EntityTypeBuilder<SponsorshipRequest> builder)
    {
        builder.ToTable(
            "sponsorship_requests",
            table => table.HasCheckConstraint(
                "ck_sponsorship_requests_requested_amount_non_negative",
                "requested_amount >= 0"));

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Title).HasMaxLength(200).IsRequired();
        builder.Property(request => request.RequestorName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.RequestorId).HasMaxLength(450).IsRequired();
        builder.Property(request => request.Department).HasMaxLength(120).IsRequired();
        builder.Property(request => request.EventName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.EventDate).HasColumnType("date").IsRequired();
        builder.Property(request => request.RequestedAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(request => request.Purpose).HasMaxLength(4000).IsRequired();
        builder.Property(request => request.ExpectedBenefit).HasMaxLength(4000);
        builder.Property(request => request.Remarks).HasMaxLength(4000);
        builder.Property(request => request.Status).HasConversion<int>().IsRequired();
        builder.Property(request => request.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(request => request.CreatedBy).HasMaxLength(450);
        builder.Property(request => request.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(request => request.UpdatedBy).HasMaxLength(450);
        builder.Property(request => request.Version).IsRowVersion();

        builder.HasOne(request => request.SponsorshipType)
            .WithMany(type => type.SponsorshipRequests)
            .HasForeignKey(request => request.SponsorshipTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => request.RequestorId);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => new { request.Status, request.CreatedAt });
        builder.HasIndex(request => request.SponsorshipTypeId);
    }
}
