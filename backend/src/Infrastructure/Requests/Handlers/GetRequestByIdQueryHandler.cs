using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.Requests;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class GetRequestByIdQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<GetRequestByIdQuery, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(GetRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var meta = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .Select(r => new { r.RequestorId, r.Status })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (meta is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        var isOwner = string.Equals(meta.RequestorId, currentUser.UserId, StringComparison.Ordinal);
        var isReviewer = currentUser.Roles.Contains(Roles.Manager)
                         || currentUser.Roles.Contains(Roles.FinanceAdmin);
        var isAdmin = currentUser.Roles.Contains(Roles.SystemAdmin);

        // Drafts are invisible to non-owners (B5): return 404 to avoid leaking existence.
        if (meta.Status == RequestStatus.Draft && !isOwner)
        {
            throw new NotFoundException("Request was not found.");
        }

        if (!isOwner && !isReviewer && !isAdmin)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        var detail = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .SelectDetailDto()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return detail ?? throw new NotFoundException("Request was not found.");
    }
}
