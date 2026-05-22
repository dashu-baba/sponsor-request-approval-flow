using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Models;

public sealed record WorkflowHistoryDto(
    long Id,
    string ActorId,
    string ActorName,
    RequestStatus FromStatus,
    RequestStatus ToStatus,
    string? Remarks,
    DateTimeOffset OccurredAt);
