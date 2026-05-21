namespace SponsorshipApproval.Application.Auth.Models;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string TokenType);
