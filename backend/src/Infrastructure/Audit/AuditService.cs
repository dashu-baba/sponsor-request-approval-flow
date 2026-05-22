using System.Text.Json;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Domain.Audit;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Audit;

public sealed class AuditService(AppDbContext dbContext) : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Record(AuditRecord record)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            OccurredAt = DateTimeOffset.UtcNow,
            ActorId = record.ActorId,
            Action = record.Action,
            Category = record.Category,
            ResourceType = record.ResourceType,
            ResourceId = record.ResourceId,
            Summary = record.Summary,
            Metadata = record.Metadata is null
                ? null
                : JsonSerializer.Serialize(record.Metadata, SerializerOptions),
        });
    }
}
