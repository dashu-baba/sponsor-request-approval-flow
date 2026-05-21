using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class GetRequestByIdQueryHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IMapper mapper) : IRequestHandler<GetRequestByIdQuery, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(GetRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Include(entity => entity.SponsorshipType)
            .SingleOrDefaultAsync(entity => entity.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        RequestMutationHelper.EnsureOwner(request, currentUser.UserId);

        return mapper.Map<RequestDetailDto>(request);
    }
}
