using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class ListOwnRequestsQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<ListOwnRequestsQuery, PagedResult<RequestListItemDto>>
{
    public async Task<PagedResult<RequestListItemDto>> Handle(
        ListOwnRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var requests = dbContext.SponsorshipRequests.AsNoTracking()
            .Where(request => request.RequestorId == currentUser.UserId);

        var totalCount = await requests.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await requests
            .OrderByDescending(request => request.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(request => new RequestListItemDto(
                request.Id,
                request.Title,
                request.Status,
                request.EventName,
                request.EventDate,
                request.RequestedAmount,
                request.SponsorshipType!.Name,
                request.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RequestListItemDto>(items, query.Page, query.PageSize, totalCount);
    }
}
