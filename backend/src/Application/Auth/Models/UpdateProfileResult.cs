namespace SponsorshipApproval.Application.Auth.Models;

public enum UpdateProfileFailureReason
{
    UserNotFound,
    IdentityValidationFailed,
    UnexpectedFailure,
}

public sealed record UpdateProfileResult(
    UpdateProfileFailureReason? FailureReason = null,
    IReadOnlyList<string>? Errors = null,
    UserProfileResponse? Profile = null)
{
    public bool Succeeded => Profile is not null;

    public static UpdateProfileResult Success(UserProfileResponse profile) => new(Profile: profile);

    public static UpdateProfileResult Failed(
        UpdateProfileFailureReason reason,
        IReadOnlyList<string>? errors = null) =>
        new(FailureReason: reason, Errors: errors);
}
