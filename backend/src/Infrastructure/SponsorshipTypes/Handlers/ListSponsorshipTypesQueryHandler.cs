using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Application.SponsorshipTypes.Queries;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class ListSponsorshipTypesQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<ListSponsorshipTypesQuery, IReadOnlyList<SponsorshipTypeDto>>
{
    public async Task<IReadOnlyList<SponsorshipTypeDto>> Handle(
        ListSponsorshipTypesQuery query,
        CancellationToken cancellationToken)
    {
        var types = dbContext.SponsorshipTypes.AsNoTracking();

        if (!currentUser.Roles.Contains(Roles.SystemAdmin))
        {
            types = types.Where(type => type.IsActive);
        }

        return await types
            .OrderByDescending(type => type.IsActive)
            .ThenBy(type => type.Name)
            .Select(SponsorshipTypeProjection.ToDto)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
