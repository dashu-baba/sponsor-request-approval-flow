using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record UpdateDraftRequestCommand(Guid Id, RequestMutationBody Body) : IRequest<RequestDetailDto>;
