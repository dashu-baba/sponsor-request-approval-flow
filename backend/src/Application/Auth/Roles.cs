namespace SponsorshipApproval.Application.Auth;

public static class Roles
{
    public const string Requestor = "Requestor";

    public const string Manager = "Manager";

    public const string FinanceAdmin = "FinanceAdmin";

    public const string SystemAdmin = "SystemAdmin";

    public static readonly IReadOnlyList<string> All =
    [
        Requestor,
        Manager,
        FinanceAdmin,
        SystemAdmin,
    ];
}
