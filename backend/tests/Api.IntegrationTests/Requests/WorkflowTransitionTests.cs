using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.Requests;

public sealed class WorkflowTransitionTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task Full_approval_path_Draft_to_Approved_should_succeed()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-happy-{suffix}@test.local";
        var managerEmail = $"mgr-happy-{suffix}@test.local";
        var financeEmail = $"fin-happy-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);
        await CreateUserAsync(financeEmail, Roles.FinanceAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        using var submitResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken);
        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await submitResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        submitted!.Status.Should().Be(RequestStatus.PendingManagerApproval);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var approveManagerResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "Looks good." }, TestContext.Current.CancellationToken);
        approveManagerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var managerApproved = await approveManagerResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        managerApproved!.Status.Should().Be(RequestStatus.PendingFinanceReview);

        using var financeClient = await AuthenticatedClientAsync(financeEmail);
        using var approveFinanceResp = await financeClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = (string?)null }, TestContext.Current.CancellationToken);
        approveFinanceResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveFinanceResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        approved!.Status.Should().Be(RequestStatus.Approved);

        await AssertHistoryCountAsync(draft.Id, expectedCount: 3);
    }

    [Fact]
    public async Task Manager_reject_should_set_status_to_Rejected_and_further_transitions_return_409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-reject-{suffix}@test.local";
        var managerEmail = $"mgr-reject-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var rejectResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "Not in budget." }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await rejectResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        rejected!.Status.Should().Be(RequestStatus.Rejected);

        using var retryResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        retryResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Finance_reject_should_set_status_to_Rejected()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-finrej-{suffix}@test.local";
        var managerEmail = $"mgr-finrej-{suffix}@test.local";
        var financeEmail = $"fin-finrej-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);
        await CreateUserAsync(financeEmail, Roles.FinanceAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        (await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var financeClient = await AuthenticatedClientAsync(financeEmail);
        using var rejectResp = await financeClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "Compliance issue." }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await rejectResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Rejected);

        await AssertHistoryCountAsync(draft.Id, expectedCount: 3);
    }

    [Fact]
    public async Task Cancel_in_Draft_should_succeed()
    {
        var requestorEmail = $"req-cncl1-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await cancelResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_in_PendingManagerApproval_should_succeed()
    {
        var requestorEmail = $"req-cncl2-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await cancelResp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken);
        result!.Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_in_PendingFinanceReview_should_return_409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-cncl3-{suffix}@test.local";
        var managerEmail = $"mgr-cncl3-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        (await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var cancelResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/cancel", new { }, TestContext.Current.CancellationToken);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reject_without_remarks_should_return_400()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-norem-{suffix}@test.local";
        var managerEmail = $"mgr-norem-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var rejectResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = (string?)null }, TestContext.Current.CancellationToken);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Wrong_role_should_return_403()
    {
        var requestorEmail = $"req-wrng-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(requestorEmail, Roles.Requestor);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var badResp = await requestorClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        badResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Self_approval_should_return_403()
    {
        // B4: a user who created the request cannot approve it even if they hold the Manager role.
        // Setup: a Requestor creates and submits; we then reassign ownership to the Manager in the DB
        // to simulate the edge-case where the same person holds both roles.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-self-{suffix}@test.local";
        var managerEmail = $"mgr-self-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        // Reassign request ownership to the Manager user (simulates same person holding both roles)
        await ReassignRequestorAsync(draft.Id, managerEmail);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        using var approveResp = await managerClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        approveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SystemAdmin_approve_should_return_403()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-adm-{suffix}@test.local";
        var adminEmail = $"adm-adm-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(adminEmail, Roles.SystemAdmin);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var adminClient = await AuthenticatedClientAsync(adminEmail);
        using var approveResp = await adminClient
            .PostAsJsonAsync($"/requests/{draft.Id}/approve", new { }, TestContext.Current.CancellationToken);
        approveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Concurrent_approve_transitions_should_never_double_transition()
    {
        // Proves optimistic-concurrency protection (xmin): two parallel approvals must not both
        // succeed. The losing writer receives 409 (xmin conflict) when both overlap at the DB commit
        // level, or 409/403 (state-machine rejection) when they run serially. Either way, only one
        // WorkflowHistory row is appended and the request state advances exactly once.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-conc-{suffix}@test.local";
        var manager1Email = $"mgr1-conc-{suffix}@test.local";
        var manager2Email = $"mgr2-conc-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(manager1Email, Roles.Manager);
        await CreateUserAsync(manager2Email, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);
        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var mgr1Client = await AuthenticatedClientAsync(manager1Email);
        using var mgr2Client = await AuthenticatedClientAsync(manager2Email);

        // Fire both in parallel — at minimum one will succeed, the loser gets 409 or 403
        var task1 = mgr1Client.PostAsJsonAsync(
            $"/requests/{draft.Id}/approve", new { remarks = "mgr1 approval" }, TestContext.Current.CancellationToken);
        var task2 = mgr2Client.PostAsJsonAsync(
            $"/requests/{draft.Id}/approve", new { remarks = "mgr2 approval" }, TestContext.Current.CancellationToken);

        using var resp1 = await task1;
        using var resp2 = await task2;

        var statuses = new[] { (int)resp1.StatusCode, (int)resp2.StatusCode };
        statuses.Should().Contain(200, "exactly one approval must succeed");
        statuses.Should().NotEqual(new[] { 200, 200 }, "double-transition must never occur");

        // Exactly one transition succeeded → submit (1) + one approve (1) = 2 history rows
        await AssertHistoryCountAsync(draft.Id, expectedCount: 2);
    }

    [Fact]
    public async Task Every_successful_transition_writes_a_WorkflowHistory_row()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var requestorEmail = $"req-hist-{suffix}@test.local";
        var managerEmail = $"mgr-hist-{suffix}@test.local";

        await CreateUserAsync(requestorEmail, Roles.Requestor);
        await CreateUserAsync(managerEmail, Roles.Manager);

        using var requestorClient = await AuthenticatedClientAsync(requestorEmail);
        var draft = await CreateDraftAsync(requestorClient);

        (await requestorClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        await AssertHistoryCountAsync(draft.Id, expectedCount: 1);

        using var managerClient = await AuthenticatedClientAsync(managerEmail);
        (await managerClient.PostAsJsonAsync($"/requests/{draft.Id}/reject", new { remarks = "No budget." }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        await AssertHistoryCountAsync(draft.Id, expectedCount: 2);
    }

    // ── Helpers ──

    private async Task<RequestDetailDto> CreateDraftAsync(HttpClient client)
    {
        using var resp = await client
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken))!;
    }

    private static object CreateMutationBody() => new
    {
        Title = "Workflow integration test request",
        Department = (string?)null,
        SponsorshipTypeId = 1L,
        EventName = "Test Event",
        EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToString("yyyy-MM-dd"),
        RequestedAmount = 1000m,
        Purpose = "Workflow integration test.",
        ExpectedBenefit = (string?)null,
        Remarks = (string?)null,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResp = await loginClient
            .PostAsJsonAsync("/auth/login", new LoginRequest(email, "Password1!"), TestContext.Current.CancellationToken);
        loginResp.EnsureSuccessStatusCode();
        var body = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private async Task CreateUserAsync(string email, string role)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = email.Split('@')[0],
            Department = "Engineering",
            EmailConfirmed = true,
        };
        var r = await userManager.CreateAsync(user, "Password1!");
        r.Succeeded.Should().BeTrue(string.Join(", ", r.Errors.Select(e => e.Description)));
        var rr = await userManager.AddToRoleAsync(user, role);
        rr.Succeeded.Should().BeTrue(string.Join(", ", rr.Errors.Select(e => e.Description)));
    }

    private async Task AssertHistoryCountAsync(long requestId, int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.WorkflowHistoryEntries
            .CountAsync(h => h.SponsorshipRequestId == requestId, TestContext.Current.CancellationToken);
        count.Should().Be(expectedCount);
    }

    private async Task ReassignRequestorAsync(long requestId, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var request = await db.SponsorshipRequests.SingleAsync(r => r.Id == requestId, TestContext.Current.CancellationToken);
        request.RequestorId = user!.Id;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
