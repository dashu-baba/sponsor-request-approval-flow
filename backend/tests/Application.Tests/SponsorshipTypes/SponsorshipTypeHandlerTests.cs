using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

namespace SponsorshipApproval.Application.Tests.SponsorshipTypes;

public sealed class SponsorshipTypeHandlerTests
{
    [Fact]
    public async Task Unique_name_rule_should_reject_duplicate_active_name()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SponsorshipTypes.Add(CreateType("Conference", isActive: true));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var act = () => CreateSponsorshipTypeCommandHandler.EnsureActiveNameIsUniqueAsync(
            dbContext,
            "conference",
            excludedId: null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>().ConfigureAwait(true);
    }

    [Fact]
    public async Task Unique_name_rule_should_allow_duplicate_inactive_name()
    {
        await using var dbContext = CreateDbContext();
        dbContext.SponsorshipTypes.Add(CreateType("Conference", isActive: false));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var act = () => CreateSponsorshipTypeCommandHandler.EnsureActiveNameIsUniqueAsync(
            dbContext,
            "conference",
            excludedId: null,
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task Delete_handler_should_soft_disable_referenced_type()
    {
        await using var dbContext = CreateDbContext();
        var type = CreateType("Referenced Type", isActive: true);
        var request = CreateRequest(type.Id);
        dbContext.SponsorshipTypes.Add(type);
        dbContext.SponsorshipRequests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var handler = new DeleteSponsorshipTypeCommandHandler(dbContext, new TestCurrentUserContext());

        await handler
            .Handle(new DeleteSponsorshipTypeCommand(type.Id), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var savedType = await dbContext.SponsorshipTypes
            .SingleAsync(entity => entity.Id == type.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var savedRequest = await dbContext.SponsorshipRequests
            .SingleAsync(entity => entity.Id == request.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        savedType.IsActive.Should().BeFalse();
        savedType.UpdatedBy.Should().Be(TestCurrentUserContext.UserIdValue);
        savedRequest.SponsorshipTypeId.Should().Be(type.Id);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static SponsorshipType CreateType(string name, bool isActive) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = TestCurrentUserContext.UserIdValue,
        };

    private static SponsorshipRequest CreateRequest(Guid sponsorshipTypeId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Unit test request",
            RequestorName = "Unit Requestor",
            RequestorId = "requestor-1",
            Department = "Engineering",
            SponsorshipTypeId = sponsorshipTypeId,
            EventName = "Unit Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount = 1000m,
            Purpose = "Exercise delete handler.",
            Status = RequestStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "requestor-1",
        };

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public const string UserIdValue = "admin-1";

        public string UserId => UserIdValue;

        public string DisplayName => "Admin";

        public IReadOnlyList<string> Roles => ["SystemAdmin"];

        public Task<string?> GetDepartmentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("IT");
    }
}
