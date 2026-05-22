using System.Linq.Expressions;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes;

internal static class SponsorshipTypeProjection
{
    public static readonly Expression<Func<SponsorshipType, SponsorshipTypeDto>> ToDto =
        type => new SponsorshipTypeDto(
            type.Id,
            type.Name,
            type.Description,
            type.IsActive,
            type.CreatedAt,
            type.UpdatedAt);
}
