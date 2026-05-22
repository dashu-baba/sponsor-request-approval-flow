using FluentAssertions;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Requests;

namespace SponsorshipApproval.Application.Tests.Requests;

public sealed class RequestVisibilityCheckerTests
{
    [Fact]
    public void Owner_reads_own_draft_should_not_throw()
    {
        var user = new FakeCurrentUserContext("owner-1", [Roles.Requestor]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.Draft,
            user);

        action.Should().NotThrow();
    }

    [Fact]
    public void Non_owner_reads_draft_should_throw_NotFoundException()
    {
        var user = new FakeCurrentUserContext("other-1", [Roles.Requestor]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.Draft,
            user);

        action.Should().Throw<NotFoundException>()
            .WithMessage("Request was not found.");
    }

    [Fact]
    public void Manager_reads_PendingManagerApproval_should_not_throw()
    {
        var user = new FakeCurrentUserContext("mgr-1", [Roles.Manager]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.PendingManagerApproval,
            user);

        action.Should().NotThrow();
    }

    [Fact]
    public void FinanceAdmin_reads_PendingFinanceReview_should_not_throw()
    {
        var user = new FakeCurrentUserContext("fin-1", [Roles.FinanceAdmin]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.PendingFinanceReview,
            user);

        action.Should().NotThrow();
    }

    [Fact]
    public void SystemAdmin_reads_PendingManagerApproval_should_not_throw()
    {
        var user = new FakeCurrentUserContext("adm-1", [Roles.SystemAdmin]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.PendingManagerApproval,
            user);

        action.Should().NotThrow();
    }

    [Fact]
    public void Unrelated_requestor_on_PendingManagerApproval_should_throw_ForbiddenException()
    {
        var user = new FakeCurrentUserContext("other-1", [Roles.Requestor]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.PendingManagerApproval,
            user);

        action.Should().Throw<ForbiddenException>()
            .WithMessage("You do not have access to this request.");
    }

    [Fact]
    public void Unrelated_requestor_on_PendingFinanceReview_should_throw_ForbiddenException()
    {
        var user = new FakeCurrentUserContext("other-1", [Roles.Requestor]);

        var action = () => RequestVisibilityChecker.EnsureCanAccess(
            "owner-1",
            RequestStatus.PendingFinanceReview,
            user);

        action.Should().Throw<ForbiddenException>()
            .WithMessage("You do not have access to this request.");
    }

    private sealed class FakeCurrentUserContext(string userId, IReadOnlyList<string> roles) : ICurrentUserContext
    {
        public string UserId { get; } = userId;

        public string DisplayName { get; } = userId;

        public IReadOnlyList<string> Roles { get; } = roles;

        public Task<string?> GetDepartmentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}
