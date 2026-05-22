namespace SponsorshipApproval.Domain.Requests;

public static class WorkflowStateMachine
{
    private static readonly Dictionary<(RequestStatus From, WorkflowAction Action, string Role), RequestStatus> Transitions =
        new()
        {
            { (RequestStatus.Draft, WorkflowAction.Submit, "Requestor"), RequestStatus.PendingManagerApproval },
            { (RequestStatus.Draft, WorkflowAction.Cancel, "Requestor"), RequestStatus.Cancelled },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Cancel, "Requestor"), RequestStatus.Cancelled },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Manager"), RequestStatus.PendingFinanceReview },
            { (RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "Manager"), RequestStatus.Rejected },
            { (RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "FinanceAdmin"), RequestStatus.Approved },
            { (RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "FinanceAdmin"), RequestStatus.Rejected },
        };

    private static readonly HashSet<WorkflowAction> OwnerActions =
    [
        WorkflowAction.Submit,
        WorkflowAction.Cancel,
    ];

    public static RequestStatus Transition(
        SponsorshipRequest request,
        WorkflowAction action,
        string actorRole,
        string actorId)
    {
        if (!Transitions.TryGetValue((request.Status, action, actorRole), out var nextStatus))
        {
            var existsForAnyRole = Transitions.Keys.Any(k => k.From == request.Status && k.Action == action);
            if (existsForAnyRole)
            {
                throw new UnauthorizedAccessException(
                    $"Role '{actorRole}' is not allowed to perform '{action}' on a request in '{request.Status}' status.");
            }

            throw new InvalidOperationException(
                $"Action '{action}' is not allowed when the request is in '{request.Status}' status.");
        }

        if (OwnerActions.Contains(action))
        {
            if (actorId != request.RequestorId)
            {
                throw new UnauthorizedAccessException("Only the request owner can perform this action.");
            }
        }
        else
        {
            if (actorId == request.RequestorId)
            {
                throw new UnauthorizedAccessException("The request owner cannot approve or reject their own request.");
            }
        }

        return nextStatus;
    }
}
