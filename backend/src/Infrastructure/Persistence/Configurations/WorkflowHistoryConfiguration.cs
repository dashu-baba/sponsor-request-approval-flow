using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class WorkflowHistoryConfiguration : IEntityTypeConfiguration<WorkflowHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowHistory> builder)
    {
        builder.ToTable("workflow_history");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.Id).UseIdentityByDefaultColumn();

        builder.Property(history => history.ActorId).HasMaxLength(450).IsRequired();
        builder.Property(history => history.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.ToStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.Remarks).HasMaxLength(4000);
        builder.Property(history => history.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne(history => history.SponsorshipRequest)
            .WithMany(request => request.WorkflowHistoryEntries)
            .HasForeignKey(history => history.SponsorshipRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => history.SponsorshipRequestId);
        builder.HasIndex(history => new { history.SponsorshipRequestId, history.OccurredAt });
        builder.HasIndex(history => history.ActorId);
    }
}
