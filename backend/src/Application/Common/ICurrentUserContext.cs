namespace SponsorshipApproval.Application.Common;

public interface ICurrentUserContext
{
    string UserId { get; }

    string DisplayName { get; }

    Task<string?> GetDepartmentAsync(CancellationToken cancellationToken = default);
}
