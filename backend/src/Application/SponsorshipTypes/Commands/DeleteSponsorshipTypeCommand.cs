using MediatR;

namespace SponsorshipApproval.Application.SponsorshipTypes.Commands;

public sealed record DeleteSponsorshipTypeCommand(Guid Id) : IRequest;
