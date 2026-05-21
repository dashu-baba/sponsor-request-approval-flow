using FluentAssertions;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Requests;

namespace SponsorshipApproval.Application.Tests.Requests;

public sealed class RequestMutationHelperTests
{
    [Theory]
    [InlineData(RequestStatus.PendingManagerApproval)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public void EnsureDraft_when_status_is_not_draft_should_throw_conflict(RequestStatus status)
    {
        var request = new SponsorshipRequest { Status = status };

        var action = () => RequestMutationHelper.EnsureDraft(request);

        action.Should().Throw<ConflictException>()
            .WithMessage("Only draft requests can be edited.");
    }

    [Fact]
    public void EnsureDraft_when_status_is_draft_should_not_throw()
    {
        var request = new SponsorshipRequest { Status = RequestStatus.Draft };

        var action = () => RequestMutationHelper.EnsureDraft(request);

        action.Should().NotThrow();
    }
}
