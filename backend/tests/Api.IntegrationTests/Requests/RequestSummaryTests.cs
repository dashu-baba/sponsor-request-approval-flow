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
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Api.IntegrationTests.Requests;

public sealed class RequestSummaryTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task Manager_summary_excludes_drafts_and_counts_submitted_requests()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-sum-mgr-{suffix}@test.local";
        var mgrEmail = $"mgr-sum-mgr-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(mgrEmail, Roles.Manager);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var submitted = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{submitted.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        await CreateDraftAsync(reqClient);

        using var mgrClient = await AuthenticatedClientAsync(mgrEmail);
        using var resp = await mgrClient.GetAsync("/requests/summary", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resp.Content.ReadFromJsonAsync<RequestSummaryDto>(TestContext.Current.CancellationToken);
        summary.Should().NotBeNull();
        summary!.Draft.Should().Be(0);
        summary.PendingManagerApproval.Should().BeGreaterThanOrEqualTo(1);
        summary.Total.Should().Be(
            summary.PendingManagerApproval
            + summary.PendingFinanceReview
            + summary.Approved
            + summary.Rejected
            + summary.Cancelled);
    }

    [Fact]
    public async Task Finance_summary_counts_pending_finance_after_manager_approval()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-sum-fin-{suffix}@test.local";
        var mgrEmail = $"mgr-sum-fin-{suffix}@test.local";
        var finEmail = $"fin-sum-fin-{suffix}@test.local";
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
        using var resp = await finClient.GetAsync("/requests/summary", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resp.Content.ReadFromJsonAsync<RequestSummaryDto>(TestContext.Current.CancellationToken);
        summary.Should().NotBeNull();
        summary!.Draft.Should().Be(0);
        summary.PendingFinanceReview.Should().BeGreaterThanOrEqualTo(1);
        summary.PendingManagerApproval.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Requestor_summary_includes_own_drafts()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-sum-own-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        await CreateDraftAsync(reqClient);
        var submitted = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{submitted.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var resp = await reqClient.GetAsync("/requests/summary", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resp.Content.ReadFromJsonAsync<RequestSummaryDto>(TestContext.Current.CancellationToken);
        summary.Should().NotBeNull();
        summary!.Draft.Should().BeGreaterThanOrEqualTo(1);
        summary.PendingManagerApproval.Should().BeGreaterThanOrEqualTo(1);
        summary.Total.Should().Be(
            summary.Draft
            + summary.PendingManagerApproval
            + summary.PendingFinanceReview
            + summary.Approved
            + summary.Rejected
            + summary.Cancelled);
    }

    [Fact]
    public async Task Admin_summary_excludes_drafts()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var reqEmail = $"req-sum-adm-{suffix}@test.local";
        var admEmail = $"adm-sum-adm-{suffix}@test.local";
        await CreateUserAsync(reqEmail, Roles.Requestor);
        await CreateUserAsync(admEmail, Roles.SystemAdmin);

        using var reqClient = await AuthenticatedClientAsync(reqEmail);
        var submitted = await CreateDraftAsync(reqClient);
        (await reqClient.PostAsJsonAsync($"/requests/{submitted.Id}/submit", new { }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        await CreateDraftAsync(reqClient);

        using var admClient = await AuthenticatedClientAsync(admEmail);
        using var resp = await admClient.GetAsync("/requests/summary", TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resp.Content.ReadFromJsonAsync<RequestSummaryDto>(TestContext.Current.CancellationToken);
        summary.Should().NotBeNull();
        summary!.Draft.Should().Be(0);
        summary.PendingManagerApproval.Should().BeGreaterThanOrEqualTo(1);
    }

    private async Task<RequestDetailDto> CreateDraftAsync(HttpClient client)
    {
        using var resp = await client.PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken))!;
    }

    private static object CreateMutationBody() => new
    {
        Title = "Summary integration test request",
        Department = (string?)null,
        SponsorshipTypeId = 1L,
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
