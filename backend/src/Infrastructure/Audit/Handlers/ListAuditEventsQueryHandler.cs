using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit.Models;
using SponsorshipApproval.Application.Audit.Queries;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Infrastructure.Audit;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Audit.Handlers;

public sealed class ListAuditEventsQueryHandler(AppDbContext dbContext)
    : IRequestHandler<ListAuditEventsQuery, PagedResult<AuditEventDto>>
{
    public async Task<PagedResult<AuditEventDto>> Handle(
        ListAuditEventsQuery query,
        CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            baseQuery = baseQuery.Where(auditEvent => auditEvent.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            baseQuery = baseQuery.Where(auditEvent => auditEvent.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorId))
        {
            var actorId = query.ActorId.Trim();
            baseQuery = baseQuery.Where(auditEvent => auditEvent.ActorId == actorId);
        }

        if (query.From.HasValue)
        {
            baseQuery = baseQuery.Where(auditEvent => auditEvent.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            baseQuery = baseQuery.Where(auditEvent => auditEvent.OccurredAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ResourceType))
        {
            var resourceType = query.ResourceType.Trim();
            baseQuery = baseQuery.Where(auditEvent => auditEvent.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(query.ResourceId))
        {
            var resourceId = query.ResourceId.Trim();
            baseQuery = baseQuery.Where(auditEvent => auditEvent.ResourceId == resourceId);
        }

        if (!string.IsNullOrWhiteSpace(query.RequestId))
        {
            var requestId = query.RequestId.Trim();
            var jsonFilter = $$"""{"requestId":"{{requestId}}"}""";
            baseQuery = baseQuery.Where(auditEvent =>
                auditEvent.Metadata != null && EF.Functions.JsonContains(auditEvent.Metadata, jsonFilter));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await baseQuery
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Join(
                dbContext.Users,
                auditEvent => auditEvent.ActorId,
                user => user.Id,
                (auditEvent, user) => AuditEventProjection.ToDto(
                    auditEvent,
                    user.DisplayName ?? user.UserName ?? auditEvent.ActorId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AuditEventDto>(items, query.Page, query.PageSize, totalCount);
    }
}
