using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class GetRequestByIdQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<GetRequestByIdQuery, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(GetRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var requestorId = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(request => request.Id == query.Id)
            .Select(request => request.RequestorId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requestorId is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        if (!string.Equals(requestorId, currentUser.UserId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        var detail = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(request => request.Id == query.Id)
            .SelectDetailDto()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return detail ?? throw new NotFoundException("Request was not found.");
    }
}
