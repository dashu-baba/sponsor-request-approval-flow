using MediatR;
using SponsorshipApproval.Application.SponsorshipTypes.Models;

namespace SponsorshipApproval.Application.SponsorshipTypes.Queries;

public sealed record ListSponsorshipTypesQuery : IRequest<IReadOnlyList<SponsorshipTypeDto>>;
