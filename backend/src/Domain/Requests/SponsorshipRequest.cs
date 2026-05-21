namespace SponsorshipApproval.Domain.Requests;

public sealed class SponsorshipRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string RequestorName { get; set; } = string.Empty;

    public string RequestorId { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public Guid SponsorshipTypeId { get; set; }

    public SponsorshipType? SponsorshipType { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public decimal RequestedAmount { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string? ExpectedBenefit { get; set; }

    public string? Remarks { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public uint Version { get; set; }

    public ICollection<WorkflowHistory> WorkflowHistoryEntries { get; } = new List<WorkflowHistory>();

    public ICollection<Attachment> Attachments { get; } = new List<Attachment>();
}
