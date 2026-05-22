using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Requests;

internal static class RequestVisibilityChecker
{
    internal static void EnsureCanAccess(
        string requestorId,
        RequestStatus requestStatus,
        ICurrentUserContext currentUser)
    {
        var isOwner = string.Equals(requestorId, currentUser.UserId, StringComparison.Ordinal);
        var isReviewer = currentUser.Roles.Contains(Roles.Manager)
                         || currentUser.Roles.Contains(Roles.FinanceAdmin);
        var isAdmin = currentUser.Roles.Contains(Roles.SystemAdmin);

        // Drafts are invisible to non-owners (B5): return 404 to avoid leaking existence.
        if (requestStatus == RequestStatus.Draft && !isOwner)
        {
            throw new NotFoundException("Request was not found.");
        }

        if (!isOwner && !isReviewer && !isAdmin)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }
    }
}
