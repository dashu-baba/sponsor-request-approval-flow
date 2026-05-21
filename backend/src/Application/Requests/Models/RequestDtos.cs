using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Models;

public sealed record RequestDetailDto(
    Guid Id,
    string Title,
    string RequestorName,
    string RequestorId,
    string Department,
    Guid SponsorshipTypeId,
    string SponsorshipTypeName,
    string EventName,
    DateOnly EventDate,
    decimal RequestedAmount,
    string Purpose,
    string? ExpectedBenefit,
    string? Remarks,
    RequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record RequestListItemDto(
    Guid Id,
    string Title,
    RequestStatus Status,
    string EventName,
    DateOnly EventDate,
    decimal RequestedAmount,
    string SponsorshipTypeName,
    DateTimeOffset CreatedAt);
