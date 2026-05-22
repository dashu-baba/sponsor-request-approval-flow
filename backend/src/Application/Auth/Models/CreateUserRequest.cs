namespace SponsorshipApproval.Application.Auth.Models;

public sealed record CreateUserRequest(
    string Email,
    string DisplayName,
    string? Department,
    string Role,
    string InitialPassword);
