using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class GetRequestHistoryQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<GetRequestHistoryQuery, IReadOnlyList<WorkflowHistoryDto>>
{
    public async Task<IReadOnlyList<WorkflowHistoryDto>> Handle(
        GetRequestHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == query.RequestId)
            .Select(r => new { r.RequestorId, r.Status })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        var isOwner = string.Equals(request.RequestorId, currentUser.UserId, StringComparison.Ordinal);
        var isReviewer = currentUser.Roles.Contains(Roles.Manager)
                         || currentUser.Roles.Contains(Roles.FinanceAdmin);
        var isAdmin = currentUser.Roles.Contains(Roles.SystemAdmin);

        if (request.Status == RequestStatus.Draft && !isOwner)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        if (!isOwner && !isReviewer && !isAdmin)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        var history = await dbContext.WorkflowHistoryEntries
            .AsNoTracking()
            .Where(h => h.SponsorshipRequestId == query.RequestId)
            .OrderBy(h => h.OccurredAt)
            .Join(
                dbContext.Users,
                h => h.ActorId,
                u => u.Id,
                (h, u) => new WorkflowHistoryDto(
                    h.Id,
                    h.ActorId,
                    u.DisplayName ?? u.UserName ?? h.ActorId,
                    h.FromStatus,
                    h.ToStatus,
                    h.Remarks,
                    h.OccurredAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return history;
    }
}
