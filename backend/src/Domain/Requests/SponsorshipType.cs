namespace SponsorshipApproval.Domain.Requests;

public sealed class SponsorshipType
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public ICollection<SponsorshipRequest> SponsorshipRequests { get; } = new List<SponsorshipRequest>();
}
