using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record TransitionBody(string? Remarks);

public sealed record SubmitRequestCommand(Guid Id) : IRequest<RequestDetailDto>;

public sealed record CancelRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record ApproveRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record RejectRequestCommand(Guid Id, string? Remarks) : IRequest<RequestDetailDto>;
