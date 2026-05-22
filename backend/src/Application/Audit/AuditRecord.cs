namespace SponsorshipApproval.Application.Audit;

public sealed record AuditRecord(
    string ActorId,
    string Action,
    string Category,
    string ResourceType,
    string ResourceId,
    string? Summary = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
