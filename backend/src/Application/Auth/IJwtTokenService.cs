namespace SponsorshipApproval.Application.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(
        string userId,
        string email,
        string displayName,
        string role,
        string securityStamp);

    DateTimeOffset GetAccessTokenExpiry();

    (string RawToken, string TokenHash) CreateRefreshToken();

    DateTimeOffset GetRefreshTokenExpiry();
}
