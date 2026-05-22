using MediatR;
using SponsorshipApproval.Application.SponsorshipTypes.Models;

namespace SponsorshipApproval.Application.SponsorshipTypes.Commands;

public sealed record UpdateSponsorshipTypeCommand(long Id, SponsorshipTypeMutationBody Body)
    : IRequest<SponsorshipTypeDto>;
