namespace SponsorshipApproval.Domain.Audit;

public sealed class AuditEvent
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string ResourceId { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? Metadata { get; set; }
}
