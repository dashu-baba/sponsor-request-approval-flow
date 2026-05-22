namespace SponsorshipApproval.Application.Auth.Models;

public enum CreateUserFailureReason
{
    DuplicateEmail,
    PolicyViolation,
    RoleAssignmentFailed,
}

public sealed record CreateUserResult(
    CreateUserFailureReason? FailureReason = null,
    IReadOnlyList<string>? PolicyErrors = null,
    UserSummaryResponse? User = null)
{
    public bool Succeeded => FailureReason is null && User is not null;

    public static CreateUserResult Success(UserSummaryResponse user) => new(User: user);

    public static CreateUserResult Failed(
        CreateUserFailureReason reason,
        IReadOnlyList<string>? policyErrors = null) =>
        new(FailureReason: reason, PolicyErrors: policyErrors);
}
