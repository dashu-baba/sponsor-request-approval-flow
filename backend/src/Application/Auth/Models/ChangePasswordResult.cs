namespace SponsorshipApproval.Application.Auth.Models;

public enum ChangePasswordFailureReason
{
    UserNotFound,
    WrongCurrentPassword,
    PolicyViolation,
}

public sealed record ChangePasswordResult(
    ChangePasswordFailureReason? FailureReason = null,
    IReadOnlyList<string>? PolicyErrors = null,
    LoginResponse? Response = null,
    string? RawRefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAt = null)
{
    public bool Succeeded => FailureReason is null && Response is not null;

    public static ChangePasswordResult Success(
        LoginResponse response,
        string rawRefreshToken,
        DateTimeOffset refreshTokenExpiresAt) =>
        new(Response: response, RawRefreshToken: rawRefreshToken, RefreshTokenExpiresAt: refreshTokenExpiresAt);

    public static ChangePasswordResult Failed(ChangePasswordFailureReason reason, IReadOnlyList<string>? policyErrors = null) =>
        new(FailureReason: reason, PolicyErrors: policyErrors);
}
