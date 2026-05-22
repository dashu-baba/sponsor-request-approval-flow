using MediatR;

namespace SponsorshipApproval.Application.SponsorshipTypes.Commands;

public sealed record DeleteSponsorshipTypeCommand(long Id) : IRequest;
