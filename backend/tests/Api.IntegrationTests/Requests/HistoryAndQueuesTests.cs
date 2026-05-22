using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Api.IntegrationTests.Requests;

public sealed class HistoryAndQueuesTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    // ── History visibility ──────────────────────────────────────────────

    [Fact]
    public async Task History_owner_can_see_their_own_submitted_request_history()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-own-{suffix}@test.local";
        var mgrEmail = $"mgr-hist-own-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var resp = await reqClient.GetAsync($"/requests/{draft.Id}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await resp.Content.ReadFromJsonAsync<IReadOnlyList<WorkflowHistoryDto>>(TestContext.Current.CancellationToken);
        history.Should().HaveCount(1);
        history![0].FromStatus.Should().Be(RequestStatus.Draft);
        history[0].ToStatus.Should().Be(RequestStatus.PendingManagerApproval);
    }

    [Fact]
    public async Task History_manager_can_see_submitted_request_history()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-mgr-{suffix}@test.local";
        var mgrEmail = $"mgr-hist-mgr-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        using var resp = await mgrClient.GetAsync($"/requests/{draft.Id}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await resp.Content.ReadFromJsonAsync<IReadOnlyList<WorkflowHistoryDto>>(TestContext.Current.CancellationToken);
        history.Should().HaveCount(1);
    }

    [Fact]
    public async Task History_finance_can_see_submitted_request_history()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-fin-{suffix}@test.local";
        var mgrEmail = $"mgr-hist-fin-{suffix}@test.local";
        var finEmail = $"fin-hist-fin-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);
        await CreateUserAsync(finEmail, Roles.FinanceAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        (await mgrClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var finClient = await AuthenticatedClientAsync(finEmail);
        using var resp = await finClient.GetAsync($"/requests/{draft.Id}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await resp.Content.ReadFromJsonAsync<IReadOnlyList<WorkflowHistoryDto>>(TestContext.Current.CancellationToken);
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task History_admin_can_see_any_submitted_request_history()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-adm-{suffix}@test.local";
        var admEmail = $"adm-hist-adm-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(admEmail, Roles.SystemAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var admClient = await AuthenticatedClientAsync(admEmail);
        using var resp = await admClient.GetAsync($"/requests/{draft.Id}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await resp.Content.ReadFromJsonAsync<IReadOnlyList<WorkflowHistoryDto>>(TestContext.Current.CancellationToken);
        history.Should().HaveCount(1);
    }

    [Fact]
    public async Task History_draft_is_404_for_non_owner()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-drft-{suffix}@test.local";
        var mgrEmail = $"mgr-hist-drft-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);

        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        using var resp = await mgrClient.GetAsync($"/requests/{draft.Id}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task History_unknown_request_returns_404()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-hist-404-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);

        using var client = await AuthenticatedClientAsync(reqEmail);
        using var resp = await client.GetAsync($"/requests/{Guid.NewGuid()}/history", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Role-scoped list queries ────────────────────────────────────────

    [Fact]
    public async Task Manager_queue_returns_only_PendingManagerApproval_requests()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-mgq-{suffix}@test.local";
        var mgrEmail = $"mgr-mgq-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);

        var submitted = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{submitted.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        await CreateDraftAsync(reqClient); // stays Draft — should NOT appear

        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        using var resp = await mgrClient.GetAsync("/requests?page=1&pageSize=50", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken);

        result!.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(RequestStatus.PendingManagerApproval));
        result.Items.Should().Contain(item => item.Id == submitted.Id);
    }

    [Fact]
    public async Task Finance_queue_returns_only_PendingFinanceReview_requests()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-finq-{suffix}@test.local";
        var mgrEmail = $"mgr-finq-{suffix}@test.local";
        var finEmail = $"fin-finq-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);
        await CreateUserAsync(finEmail, Roles.FinanceAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        (await mgrClient.PostAsJsonAsync($"/requests/{draft.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var finClient = await AuthenticatedClientAsync(finEmail);
        using var resp = await finClient.GetAsync("/requests?page=1&pageSize=50", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken);

        result!.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(RequestStatus.PendingFinanceReview));
        result.Items.Should().Contain(item => item.Id == draft.Id);
    }

    [Fact]
    public async Task Admin_list_returns_all_submitted_no_drafts()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-admq-{suffix}@test.local";
        var mgrEmail = $"mgr-admq-{suffix}@test.local";
        var admEmail = $"adm-admq-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);
        await CreateUserAsync(admEmail, Roles.SystemAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);

        // Draft — should NOT appear
        await CreateDraftAsync(reqClient);

        // Submitted — should appear
        var submitted = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{submitted.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        // PendingFinanceReview — should appear
        var toApprove = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{toApprove.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        (await mgrClient.PostAsJsonAsync($"/requests/{toApprove.Id}/approve", new { remarks = "OK" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var admClient = await AuthenticatedClientAsync(admEmail);
        using var resp = await admClient.GetAsync("/requests?page=1&pageSize=100", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken);

        result!.Items.Should().NotContain(item => item.Status == RequestStatus.Draft);
        result.Items.Should().Contain(item => item.Id == submitted.Id);
        result.Items.Should().Contain(item => item.Id == toApprove.Id);
    }

    [Fact]
    public async Task Admin_list_status_filter_returns_only_matching_status()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-admf-{suffix}@test.local";
        var admEmail = $"adm-admf-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(admEmail, Roles.SystemAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{draft.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var admClient = await AuthenticatedClientAsync(admEmail);
        using var resp = await admClient.GetAsync(
            $"/requests?status={(int)RequestStatus.PendingManagerApproval}",
            TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken);
        result!.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(RequestStatus.PendingManagerApproval));
    }

    [Fact]
    public async Task Requestor_list_still_returns_own_requests()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-own-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var draft = await CreateDraftAsync(reqClient);

        using var resp = await reqClient.GetAsync("/requests?page=1&pageSize=20", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken);
        result!.Items.Should().Contain(item => item.Id == draft.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<RequestDetailDto> CreateDraftAsync(HttpClient client)
    {
        using var resp = await client.PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken))!;
    }

    private static object CreateMutationBody() => new
    {
        Title = "History/queue integration test request",
        Department = (string?)null,
        SponsorshipTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
        EventName = "Test Event",
        EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToString("yyyy-MM-dd"),
        RequestedAmount = 500m,
        Purpose = "Integration test purpose.",
        ExpectedBenefit = (string?)null,
        Remarks = (string?)null,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResp = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, "Password1!"),
            TestContext.Current.CancellationToken);
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
}
