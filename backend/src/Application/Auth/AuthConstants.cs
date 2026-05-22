namespace SponsorshipApproval.Application.Auth;

public static class AuthConstants
{
    public const string RefreshTokenCookieName = "refresh_token";

    public const string RefreshTokenCookiePath = "/api/auth";

    public const string SecurityStampClaimType = "security_stamp";

    public const int DisplayNameMaxLength = 120;

    public const int DepartmentMaxLength = 120;
}
