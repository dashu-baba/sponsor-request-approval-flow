namespace SponsorshipApproval.Application.Audit.Models;

public sealed record AuditEventDto(
    long Id,
    DateTimeOffset OccurredAt,
    string ActorId,
    string ActorDisplayName,
    string Action,
    string Category,
    string ResourceType,
    string ResourceId,
    string? Summary,
    IReadOnlyDictionary<string, object?>? Metadata);
