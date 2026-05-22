namespace SponsorshipApproval.Application.Auth.Models;

public sealed record UserSummaryResponse(
    string Id,
    string Email,
    string DisplayName,
    string? Department,
    string Role);
