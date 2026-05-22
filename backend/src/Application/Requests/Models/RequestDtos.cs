using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Models;

public sealed record RequestDetailDto(
    long Id,
    string Title,
    string RequestorName,
    string RequestorId,
    string Department,
    long SponsorshipTypeId,
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
    long Id,
    string Title,
    string RequestorName,
    string Department,
    RequestStatus Status,
    string EventName,
    DateOnly EventDate,
    decimal RequestedAmount,
    string SponsorshipTypeName,
    DateTimeOffset CreatedAt);
