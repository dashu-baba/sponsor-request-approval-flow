using MediatR;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Commands;

public sealed record TransitionBody(string? Remarks);

public sealed record SubmitRequestCommand(long Id) : IRequest<RequestDetailDto>;

public sealed record CancelRequestCommand(long Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record ApproveRequestCommand(long Id, string? Remarks) : IRequest<RequestDetailDto>;

public sealed record RejectRequestCommand(long Id, string? Remarks) : IRequest<RequestDetailDto>;
