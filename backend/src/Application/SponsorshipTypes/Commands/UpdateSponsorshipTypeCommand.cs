using MediatR;
using SponsorshipApproval.Application.SponsorshipTypes.Models;

namespace SponsorshipApproval.Application.SponsorshipTypes.Commands;

public sealed record UpdateSponsorshipTypeCommand(Guid Id, SponsorshipTypeMutationBody Body)
    : IRequest<SponsorshipTypeDto>;
