using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record UpdateDraftRequestCommand(long Id, RequestMutationBody Body) : IRequest<RequestDetailDto>;
