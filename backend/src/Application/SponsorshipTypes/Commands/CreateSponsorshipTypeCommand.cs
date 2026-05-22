using MediatR;
using SponsorshipApproval.Application.SponsorshipTypes.Models;

namespace SponsorshipApproval.Application.SponsorshipTypes.Commands;

public sealed record CreateSponsorshipTypeCommand(SponsorshipTypeMutationBody Body) : IRequest<SponsorshipTypeDto>;
