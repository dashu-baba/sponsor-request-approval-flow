using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
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

        RequestVisibilityChecker.EnsureCanAccess(meta.RequestorId, meta.Status, currentUser);

        var detail = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .SelectDetailDto()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return detail ?? throw new NotFoundException("Request was not found.");
    }
}
