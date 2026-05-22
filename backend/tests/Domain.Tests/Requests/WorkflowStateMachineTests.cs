using FluentAssertions;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Domain.Tests.Requests;

public sealed class WorkflowStateMachineTests
{
    [Fact]
    public void WorkflowAction_enum_should_define_expected_values()
    {
        Enum.GetValues<WorkflowAction>()
            .Should().BeEquivalentTo(new[]
            {
                WorkflowAction.Submit,
                WorkflowAction.Cancel,
                WorkflowAction.Approve,
                WorkflowAction.Reject,
            });
    }

    // Owner actions: actorId must match RequestorId
    [Theory]
    [InlineData(RequestStatus.Draft, WorkflowAction.Submit, "Requestor", RequestStatus.PendingManagerApproval)]
    [InlineData(RequestStatus.Draft, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled)]
    public void Valid_owner_transitions_should_return_correct_next_status(
        RequestStatus from, WorkflowAction action, string actorRole, RequestStatus expected)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "actor-1" };
        var result = WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        result.Should().Be(expected);
    }

    // Review actions: actorId must differ from RequestorId (no self-approval)
    [Theory]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Manager", RequestStatus.PendingFinanceReview)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "Manager", RequestStatus.Rejected)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "FinanceAdmin", RequestStatus.Approved)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "FinanceAdmin", RequestStatus.Rejected)]
    public void Valid_review_transitions_should_return_correct_next_status(
        RequestStatus from, WorkflowAction action, string actorRole, RequestStatus expected)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "requestor-1" };
        var result = WorkflowStateMachine.Transition(request, action, actorRole, actorId: "reviewer-1");
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Cancel, "Requestor")]
    [InlineData(RequestStatus.Approved, WorkflowAction.Approve, "FinanceAdmin")]
    [InlineData(RequestStatus.Rejected, WorkflowAction.Approve, "Manager")]
    [InlineData(RequestStatus.Cancelled, WorkflowAction.Submit, "Requestor")]
    public void Invalid_state_transitions_should_throw_InvalidOperationException(
        RequestStatus from, WorkflowAction action, string actorRole)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "actor-1" };
        var act = () => WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Requestor")]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "FinanceAdmin")]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "Manager")]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "Requestor")]
    [InlineData(RequestStatus.Draft, WorkflowAction.Submit, "SystemAdmin")]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "SystemAdmin")]
    public void Wrong_role_transitions_should_throw_UnauthorizedAccessException(
        RequestStatus from, WorkflowAction action, string actorRole)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "other-user" };
        var act = () => WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Self_approval_should_throw_UnauthorizedAccessException()
    {
        var request = new SponsorshipRequest
        {
            Status = RequestStatus.PendingManagerApproval,
            RequestorId = "manager-user",
        };
        var act = () => WorkflowStateMachine.Transition(
            request, WorkflowAction.Approve, "Manager", actorId: "manager-user");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Owner_submit_from_different_user_should_throw_UnauthorizedAccessException()
    {
        var request = new SponsorshipRequest
        {
            Status = RequestStatus.Draft,
            RequestorId = "owner-user",
        };
        var act = () => WorkflowStateMachine.Transition(
            request, WorkflowAction.Submit, "Requestor", actorId: "not-owner");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Cancel_by_non_owner_should_throw_UnauthorizedAccessException()
    {
        var request = new SponsorshipRequest
        {
            Status = RequestStatus.Draft,
            RequestorId = "owner-user",
        };
        var act = () => WorkflowStateMachine.Transition(
            request, WorkflowAction.Cancel, "Requestor", actorId: "not-owner");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData(RequestStatus.Draft, WorkflowAction.Submit, "Requestor", RequestStatus.PendingManagerApproval, true)]
    [InlineData(RequestStatus.Draft, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Cancel, "Requestor", RequestStatus.Cancelled, true)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Approve, "Manager", RequestStatus.PendingFinanceReview, false)]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Reject, "Manager", RequestStatus.Rejected, false)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Approve, "FinanceAdmin", RequestStatus.Approved, false)]
    [InlineData(RequestStatus.PendingFinanceReview, WorkflowAction.Reject, "FinanceAdmin", RequestStatus.Rejected, false)]
    public void All_valid_transitions_from_state_machine_table(
        RequestStatus from,
        WorkflowAction action,
        string actorRole,
        RequestStatus expected,
        bool isOwnerAction)
    {
        const string ownerId = "owner-1";
        const string reviewerId = "reviewer-1";
        var request = new SponsorshipRequest { Status = from, RequestorId = ownerId };
        var actorId = isOwnerAction ? ownerId : reviewerId;

        var result = WorkflowStateMachine.Transition(request, action, actorRole, actorId);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(RequestStatus.Approved, WorkflowAction.Approve, "FinanceAdmin")]
    [InlineData(RequestStatus.Draft, WorkflowAction.Approve, "Manager")]
    [InlineData(RequestStatus.PendingManagerApproval, WorkflowAction.Submit, "Requestor")]
    [InlineData(RequestStatus.Cancelled, WorkflowAction.Cancel, "Requestor")]
    public void Invalid_transition_pairs_should_throw(
        RequestStatus from,
        WorkflowAction action,
        string actorRole)
    {
        var request = new SponsorshipRequest { Status = from, RequestorId = "actor-1" };
        var act = () => WorkflowStateMachine.Transition(request, action, actorRole, actorId: "actor-1");

        var exception = act.Should().Throw<Exception>().Which;
        (exception is InvalidOperationException or UnauthorizedAccessException).Should().BeTrue();
    }
}
