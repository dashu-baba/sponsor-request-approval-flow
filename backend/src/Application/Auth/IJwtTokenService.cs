namespace SponsorshipApproval.Application.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(string userId, string email, string displayName, string role);

    DateTimeOffset GetAccessTokenExpiry();

    (string RawToken, string TokenHash) CreateRefreshToken();

    DateTimeOffset GetRefreshTokenExpiry();
}
