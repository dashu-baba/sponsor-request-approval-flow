using System.Text.Json;
using SponsorshipApproval.Application.Audit.Models;
using SponsorshipApproval.Domain.Audit;

namespace SponsorshipApproval.Infrastructure.Audit;

internal static class AuditEventProjection
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static AuditEventDto ToDto(
        AuditEvent auditEvent,
        string actorDisplayName)
    {
        IReadOnlyDictionary<string, object?>? metadata = null;
        if (!string.IsNullOrWhiteSpace(auditEvent.Metadata))
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                auditEvent.Metadata,
                SerializerOptions);
        }

        return new AuditEventDto(
            auditEvent.Id,
            auditEvent.OccurredAt,
            auditEvent.ActorId,
            actorDisplayName,
            auditEvent.Action,
            auditEvent.Category,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.Summary,
            metadata);
    }
}
