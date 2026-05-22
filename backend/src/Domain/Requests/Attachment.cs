namespace SponsorshipApproval.Domain.Requests;

public sealed class Attachment
{
    public long Id { get; set; }

    public long SponsorshipRequestId { get; set; }

    public SponsorshipRequest? SponsorshipRequest { get; set; }

    public string ObjectKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
}
