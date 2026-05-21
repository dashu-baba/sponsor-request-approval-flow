using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.Seeding;

public sealed class SeedDataTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private const string DefaultPassword = "Password1!";

    private static readonly (string Email, string Role, string DisplayName)[] ExpectedUsers =
    [
        ("requestor@demo.local", Roles.Requestor, "Alex Requestor"),
        ("manager@demo.local", Roles.Manager, "Morgan Manager"),
        ("finance@demo.local", Roles.FinanceAdmin, "Finley Finance"),
        ("admin@demo.local", Roles.SystemAdmin, "Sam Admin"),
    ];

    [Fact]
    public async Task Seeded_database_should_include_one_user_per_role_and_requests_in_every_status()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var (email, role, displayName) in ExpectedUsers)
        {
            var user = await userManager.FindByEmailAsync(email).ConfigureAwait(true);
            user.Should().NotBeNull($"seed user '{email}' should exist");
            user!.DisplayName.Should().Be(displayName);

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(true);
            roles.Should().ContainSingle().Which.Should().Be(role);
        }

        var requestsByStatus = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .GroupBy(request => request.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        foreach (var status in Enum.GetValues<RequestStatus>())
        {
            requestsByStatus.Should().Contain(
                entry => entry.Key == status && entry.Count >= 1,
                $"at least one seeded request should exist in status {status}");
        }

        var pendingManagerRequest = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Include(request => request.WorkflowHistoryEntries)
            .SingleAsync(
                request => request.Status == RequestStatus.PendingManagerApproval,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        pendingManagerRequest.WorkflowHistoryEntries.Should().HaveCount(1);
        pendingManagerRequest.WorkflowHistoryEntries.Single().ToStatus.Should().Be(RequestStatus.PendingManagerApproval);

        var approvedRequest = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Include(request => request.WorkflowHistoryEntries)
            .SingleAsync(request => request.Status == RequestStatus.Approved, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        approvedRequest.WorkflowHistoryEntries.Should().HaveCount(3);
        approvedRequest.WorkflowHistoryEntries
            .OrderBy(entry => entry.OccurredAt)
            .Select(entry => entry.ToStatus)
            .Should()
            .Equal(
                RequestStatus.PendingManagerApproval,
                RequestStatus.PendingFinanceReview,
                RequestStatus.Approved);

        var draftRequest = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Include(request => request.WorkflowHistoryEntries)
            .SingleAsync(request => request.Status == RequestStatus.Draft, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        draftRequest.WorkflowHistoryEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Seeded_users_should_login_with_documented_test_password()
    {
        using var client = factory.CreateClient();

        foreach (var (email, _, _) in ExpectedUsers)
        {
            using var response = await client.PostAsJsonAsync(
                "/auth/login",
                new LoginRequest(email, DefaultPassword),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"login should succeed for {email}");
        }
    }

    [Fact]
    public async Task Running_seed_twice_should_not_duplicate_roles_users_types_or_requests()
    {
        await factory.Services.SeedDatabaseAsync().ConfigureAwait(true);
        await factory.Services.SeedDatabaseAsync().ConfigureAwait(true);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var role in Roles.All)
        {
            (await roleManager.RoleExistsAsync(role).ConfigureAwait(true)).Should().BeTrue();
        }

        foreach (var (email, _, _) in ExpectedUsers)
        {
            var users = await userManager.Users
                .Where(user => user.Email == email)
                .ToListAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            users.Should().ContainSingle();
        }

        (await dbContext.SponsorshipTypes.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Should().Be(4);

        (await dbContext.SponsorshipRequests.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Should().Be(6);

        (await dbContext.WorkflowHistoryEntries.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Should().Be(9);
    }
}
