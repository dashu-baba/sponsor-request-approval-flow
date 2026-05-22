using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SponsorshipApproval.Domain.Audit;

namespace SponsorshipApproval.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.Id).UseIdentityByDefaultColumn();
        builder.Property(auditEvent => auditEvent.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(auditEvent => auditEvent.ActorId).HasMaxLength(450).IsRequired();
        builder.Property(auditEvent => auditEvent.Action).HasMaxLength(100).IsRequired();
        builder.Property(auditEvent => auditEvent.Category).HasMaxLength(50).IsRequired();
        builder.Property(auditEvent => auditEvent.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(auditEvent => auditEvent.ResourceId).HasMaxLength(100).IsRequired();
        builder.Property(auditEvent => auditEvent.Summary).HasMaxLength(500);
        builder.Property(auditEvent => auditEvent.Metadata).HasColumnType("jsonb");

        builder.HasIndex(auditEvent => auditEvent.OccurredAt);
        builder.HasIndex(auditEvent => new { auditEvent.ActorId, auditEvent.OccurredAt });
        builder.HasIndex(auditEvent => new { auditEvent.Action, auditEvent.OccurredAt });
        builder.HasIndex(auditEvent => new { auditEvent.ResourceType, auditEvent.ResourceId, auditEvent.OccurredAt });
    }
}
