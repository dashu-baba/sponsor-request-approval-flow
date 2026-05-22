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
}
