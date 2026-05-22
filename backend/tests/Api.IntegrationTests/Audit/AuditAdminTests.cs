using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Audit.Models;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.Audit;

public sealed class AuditAdminTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task SystemAdmin_should_list_audit_events()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .GetAsync("/audit?page=1&pageSize=20", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content
            .ReadFromJsonAsync<PagedResult<AuditEventDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page.Should().NotBeNull();
        page!.Items.Should().NotBeNull();
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(SeedCredentials.RequestorEmail)]
    [InlineData(SeedCredentials.ManagerEmail)]
    [InlineData(SeedCredentials.FinanceEmail)]
    public async Task Non_system_admin_should_be_forbidden(string email)
    {
        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .GetAsync("/audit", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_user_should_be_unauthorized()
    {
        using var client = factory.CreateClient();
        using var response = await client
            .GetAsync("/audit", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_request_should_write_request_created_audit_event()
    {
        var email = $"audit-requestor-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor, "Engineering").ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var createResponse = await client
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        created.Should().NotBeNull();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEvent = await dbContext.AuditEvents
            .AsNoTracking()
            .SingleAsync(
                entry => entry.Action == AuditActions.RequestCreated && entry.ResourceId == created!.Id.ToString(),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        auditEvent.Category.Should().Be(AuditCategories.Request);
        auditEvent.ResourceType.Should().Be(AuditResourceTypes.SponsorshipRequest);
        auditEvent.Metadata.Should().Contain("requestId");
        auditEvent.Metadata.Should().Contain(created!.Id.ToString());
    }

    [Fact]
    public async Task Submit_request_should_write_request_submitted_audit_event()
    {
        var email = $"audit-submit-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor, "Engineering").ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var createResponse = await client
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var beforeCount = await CountAuditEventsAsync().ConfigureAwait(true);

        using var submitResponse = await client
            .PostAsJsonAsync($"/requests/{created!.Id}/submit", new { }, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCount = await CountAuditEventsAsync().ConfigureAwait(true);
        afterCount.Should().Be(beforeCount + 1);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditEvent = await dbContext.AuditEvents
            .AsNoTracking()
            .SingleAsync(
                entry => entry.Action == AuditActions.RequestSubmitted && entry.ResourceId == created.Id.ToString(),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        auditEvent.Category.Should().Be(AuditCategories.Request);
        auditEvent.ResourceType.Should().Be(AuditResourceTypes.SponsorshipRequest);
        auditEvent.Metadata.Should().Contain("Draft");
        auditEvent.Metadata.Should().Contain("PendingManagerApproval");

        var historyCount = await dbContext.WorkflowHistoryEntries
            .CountAsync(entry => entry.SponsorshipRequestId == created.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        historyCount.Should().Be(1);
    }

    [Fact]
    public async Task Admin_create_user_should_write_user_created_audit_event()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var email = $"audit-admin-created-{Guid.NewGuid():N}@test.local";
        var beforeCount = await CountAuditEventsAsync().ConfigureAwait(true);

        using var createResponse = await client.PostAsJsonAsync(
            "/users",
            new CreateUserRequest(email, "Audit Created", "IT", Roles.Requestor, SeedCredentials.Password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var afterCount = await CountAuditEventsAsync().ConfigureAwait(true);
        afterCount.Should().Be(beforeCount + 1);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEvent = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(entry => entry.Action == AuditActions.UserCreated)
            .OrderByDescending(entry => entry.Id)
            .FirstAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        auditEvent.Metadata.Should().Contain(email);
        auditEvent.Metadata.Should().NotContain(SeedCredentials.Password);
    }

    [Fact]
    public async Task Audit_list_should_filter_by_action()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .GetAsync($"/audit?action={AuditActions.AuthLogin}&page=1&pageSize=5", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content
            .ReadFromJsonAsync<PagedResult<AuditEventDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page!.Items.Should().OnlyContain(item => item.Action == AuditActions.AuthLogin);
    }

    private static RequestMutationBody CreateMutationBody() =>
        new(
            Title: "Audit integration test request",
            Department: null,
            SponsorshipTypeId: 1,
            EventName: "Audit Event",
            EventDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount: 1500m,
            Purpose: "Verify audit trail.",
            ExpectedBenefit: null,
            Remarks: null);

    private async Task CreateUserAsync(string email, string password, string role, string? department = null)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = email.Split('@')[0],
            Department = department,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, password).ConfigureAwait(true);
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role).ConfigureAwait(true);
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(error => error.Description)));
    }

    private async Task<int> CountAuditEventsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.AuditEvents.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }

    private static class SeedCredentials
    {
        public const string AdminEmail = "admin@demo.local";

        public const string RequestorEmail = "requestor@demo.local";

        public const string ManagerEmail = "manager@demo.local";

        public const string FinanceEmail = "finance@demo.local";

        public const string Password = "Password1!";
    }
}
