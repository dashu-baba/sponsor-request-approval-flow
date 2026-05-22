using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class ListFinanceQueueQueryHandler(AppDbContext dbContext)
    : IRequestHandler<ListFinanceQueueQuery, PagedResult<RequestListItemDto>>
{
    public async Task<PagedResult<RequestListItemDto>> Handle(
        ListFinanceQueueQuery query,
        CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Status == RequestStatus.PendingFinanceReview);

        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new RequestListItemDto(
                r.Id,
                r.Title,
                r.Status,
                r.EventName,
                r.EventDate,
                r.RequestedAmount,
                r.SponsorshipType!.Name,
                r.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RequestListItemDto>(items, query.Page, query.PageSize, totalCount);
    }
}
