using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Queries;

public sealed record GetRequestByIdQuery(Guid Id) : IRequest<RequestDetailDto>;
