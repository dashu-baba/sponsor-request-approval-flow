using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class GetRequestSummaryQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<GetRequestSummaryQuery, RequestSummaryDto>
{
    public async Task<RequestSummaryDto> Handle(
        GetRequestSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.SponsorshipRequests.AsNoTracking();

        if (IsReviewerOrAdmin(currentUser.Roles))
        {
            baseQuery = baseQuery.Where(request => request.Status != RequestStatus.Draft);
        }
        else
        {
            baseQuery = baseQuery.Where(request => request.RequestorId == currentUser.UserId);
        }

        var counts = await baseQuery
            .GroupBy(request => request.Status)
            .Select(group => new StatusCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var draft = CountFor(counts, RequestStatus.Draft);
        var pendingManager = CountFor(counts, RequestStatus.PendingManagerApproval);
        var pendingFinance = CountFor(counts, RequestStatus.PendingFinanceReview);
        var approved = CountFor(counts, RequestStatus.Approved);
        var rejected = CountFor(counts, RequestStatus.Rejected);
        var cancelled = CountFor(counts, RequestStatus.Cancelled);

        return new RequestSummaryDto(
            draft + pendingManager + pendingFinance + approved + rejected + cancelled,
            draft,
            pendingManager,
            pendingFinance,
            approved,
            rejected,
            cancelled);
    }

    private static bool IsReviewerOrAdmin(IReadOnlyCollection<string> roles) =>
        roles.Contains(Roles.Manager)
        || roles.Contains(Roles.FinanceAdmin)
        || roles.Contains(Roles.SystemAdmin);

    private static int CountFor(IReadOnlyList<StatusCount> counts, RequestStatus status)
    {
        foreach (var entry in counts)
        {
            if (entry.Status == status)
            {
                return entry.Count;
            }
        }

        return 0;
    }

    private sealed record StatusCount(RequestStatus Status, int Count);
}
