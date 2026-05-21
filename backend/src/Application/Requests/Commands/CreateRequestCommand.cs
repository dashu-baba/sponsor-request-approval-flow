using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record CreateRequestCommand(RequestMutationBody Body) : IRequest<RequestDetailDto>;
