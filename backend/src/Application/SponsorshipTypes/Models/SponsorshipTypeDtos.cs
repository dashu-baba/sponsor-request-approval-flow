namespace SponsorshipApproval.Application.SponsorshipTypes.Models;

public sealed record SponsorshipTypeDto(
    long Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SponsorshipTypeMutationBody(
    string Name,
    string? Description);
