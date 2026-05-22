using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Application.SponsorshipTypes.Queries;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class ListSponsorshipTypesQueryHandler(AppDbContext dbContext)
    : IRequestHandler<ListSponsorshipTypesQuery, IReadOnlyList<SponsorshipTypeDto>>
{
    public async Task<IReadOnlyList<SponsorshipTypeDto>> Handle(
        ListSponsorshipTypesQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.SponsorshipTypes
            .AsNoTracking()
            .OrderByDescending(type => type.IsActive)
            .ThenBy(type => type.Name)
            .Select(SponsorshipTypeProjection.ToDto)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
