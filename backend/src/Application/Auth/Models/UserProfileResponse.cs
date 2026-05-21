namespace SponsorshipApproval.Application.Auth.Models;

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string DisplayName,
    string? Department,
    string Role);
