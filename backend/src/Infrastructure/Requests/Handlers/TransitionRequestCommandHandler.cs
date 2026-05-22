using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class TransitionRequestCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<SubmitRequestCommand, RequestDetailDto>,
      IRequestHandler<CancelRequestCommand, RequestDetailDto>,
      IRequestHandler<ApproveRequestCommand, RequestDetailDto>,
      IRequestHandler<RejectRequestCommand, RequestDetailDto>
{
    public Task<RequestDetailDto> Handle(SubmitRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Submit, remarks: null, cancellationToken);

    public Task<RequestDetailDto> Handle(CancelRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Cancel, command.Remarks, cancellationToken);

    public Task<RequestDetailDto> Handle(ApproveRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Approve, command.Remarks, cancellationToken);

    public Task<RequestDetailDto> Handle(RejectRequestCommand command, CancellationToken cancellationToken)
        => ApplyTransitionAsync(command.Id, WorkflowAction.Reject, command.Remarks, cancellationToken);

    private async Task<RequestDetailDto> ApplyTransitionAsync(
        long requestId,
        WorkflowAction action,
        string? remarks,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        // B3: SystemAdmin is not part of the approval chain
        var actorRole = currentUser.Roles.Single();
        if (actorRole == Roles.SystemAdmin)
        {
            throw new ForbiddenException("System administrators are not part of the approval chain.");
        }

        RequestStatus nextStatus;
        try
        {
            nextStatus = WorkflowStateMachine.Transition(request, action, actorRole, currentUser.UserId);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ForbiddenException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        var from = request.Status;
        var now = DateTimeOffset.UtcNow;

        request.Status = nextStatus;
        request.UpdatedAt = now;
        request.UpdatedBy = currentUser.UserId;

        dbContext.WorkflowHistoryEntries.Add(new WorkflowHistory
        {
            SponsorshipRequestId = request.Id,
            ActorId = currentUser.UserId,
            FromStatus = from,
            ToStatus = nextStatus,
            Remarks = remarks,
            OccurredAt = now,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A concurrent transition was already applied to this request. Please reload and try again.");
        }

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
