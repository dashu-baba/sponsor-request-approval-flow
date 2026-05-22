namespace SponsorshipApproval.Domain.Requests;

public sealed class WorkflowHistory
{
    public long Id { get; set; }

    public long SponsorshipRequestId { get; set; }

    public SponsorshipRequest? SponsorshipRequest { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public RequestStatus FromStatus { get; set; }

    public RequestStatus ToStatus { get; set; }

    public string? Remarks { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
