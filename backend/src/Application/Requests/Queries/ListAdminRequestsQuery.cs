using MediatR;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Queries;

public sealed record ListAdminRequestsQuery(
    int Page = 1,
    int PageSize = RequestValidationConstants.DefaultPageSize,
    RequestStatus? Status = null)
    : IRequest<PagedResult<RequestListItemDto>>;
