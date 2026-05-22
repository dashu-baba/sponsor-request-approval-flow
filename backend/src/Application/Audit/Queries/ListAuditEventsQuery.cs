using MediatR;
using SponsorshipApproval.Application.Audit.Models;
using SponsorshipApproval.Application.Common;

namespace SponsorshipApproval.Application.Audit.Queries;

public sealed record ListAuditEventsQuery(
    int Page = 1,
    int PageSize = RequestValidationConstants.DefaultPageSize,
    string? Action = null,
    string? Category = null,
    string? ActorId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? ResourceType = null,
    string? ResourceId = null,
    string? RequestId = null)
    : IRequest<PagedResult<AuditEventDto>>;
