namespace SponsorshipApproval.Application.Requests.Models;

public sealed record RequestMutationBody(
    string Title,
    string? Department,
    Guid SponsorshipTypeId,
    string EventName,
    DateOnly EventDate,
    decimal RequestedAmount,
    string Purpose,
    string? ExpectedBenefit,
    string? Remarks);
