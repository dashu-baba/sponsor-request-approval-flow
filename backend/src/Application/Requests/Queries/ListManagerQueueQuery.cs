using MediatR;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Queries;

public sealed record ListManagerQueueQuery(int Page = 1, int PageSize = RequestValidationConstants.DefaultPageSize)
    : IRequest<PagedResult<RequestListItemDto>>;
