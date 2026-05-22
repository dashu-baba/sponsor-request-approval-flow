using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Domain.Audit;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.Audit;

public sealed class AuditWriteTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly byte[] MinimalPdfBytes = "%PDF-1.4\n%EOF\n"u8.ToArray();

    [Fact]
    public async Task Update_draft_should_write_request_updated_audit_event()
    {
        var email = $"audit-update-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor, "Engineering").ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password).ConfigureAwait(true);
        var createBody = CreateMutationBody();
        using var createResponse = await client
            .PostAsJsonAsync("/requests", createBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var updateBody = createBody with { Title = "Updated audit title", RequestedAmount = 2500m };
        using var updateResponse = await client
            .PutAsJsonAsync($"/requests/{created!.Id}", updateBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditEvent = await FindLatestAuditAsync(AuditActions.RequestUpdated).ConfigureAwait(true);
        auditEvent.ResourceId.Should().Be(created.Id.ToString());
        auditEvent.Metadata.Should().Contain("changedFields");
        auditEvent.Metadata.Should().Contain(nameof(SponsorshipRequest.Title));
    }

    [Fact]
    public async Task Upload_attachment_should_write_attachment_uploaded_audit_event()
    {
        var email = $"audit-upload-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor, "Engineering").ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password).ConfigureAwait(true);
        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(client, requestId, MinimalPdfBytes, "audit-doc.pdf")
            .ConfigureAwait(true);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var auditEvent = await FindLatestAuditAsync(AuditActions.AttachmentUploaded).ConfigureAwait(true);
        auditEvent.Category.Should().Be(AuditCategories.Attachment);
        auditEvent.Metadata.Should().Contain("requestId");
        auditEvent.Metadata.Should().Contain(requestId.ToString());
        auditEvent.Metadata.Should().Contain("audit-doc.pdf");
    }

    [Fact]
    public async Task Sponsorship_type_mutations_should_write_audit_events()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var createBody = new SponsorshipTypeMutationBody(
            Name: $"Audit Type {Guid.NewGuid():N}",
            Description: "Audit write test");

        using var createResponse = await client
            .PostAsJsonAsync("/sponsorship-types", createBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content
            .ReadFromJsonAsync<SponsorshipTypeDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        (await FindLatestAuditAsync(AuditActions.SponsorshipTypeCreated).ConfigureAwait(true))
            .ResourceId.Should().Be(created!.Id.ToString());

        var updateBody = createBody with { Name = $"{createBody.Name} Updated" };
        using var updateResponse = await client
            .PutAsJsonAsync($"/sponsorship-types/{created.Id}", updateBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await FindLatestAuditAsync(AuditActions.SponsorshipTypeUpdated).ConfigureAwait(true))
            .ResourceId.Should().Be(created.Id.ToString());

        using var deleteResponse = await client
            .DeleteAsync($"/sponsorship-types/{created.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await FindLatestAuditAsync(AuditActions.SponsorshipTypeDeactivated).ConfigureAwait(true))
            .ResourceId.Should().Be(created.Id.ToString());
    }

    [Fact]
    public async Task Logout_should_write_auth_logout_audit_event()
    {
        var email = $"audit-logout-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor).ConfigureAwait(true);

        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, SeedCredentials.Password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();
        loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders).Should().BeTrue();

        using var logoutClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        foreach (var cookieHeader in setCookieHeaders!)
        {
            logoutClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var logoutResponse = await logoutClient
            .PostAsync("/auth/logout", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var auditEvent = await FindLatestAuditAsync(AuditActions.AuthLogout).ConfigureAwait(true);
        auditEvent.Category.Should().Be(AuditCategories.Auth);
    }

    [Fact]
    public async Task Profile_update_should_write_auth_profile_updated_audit_event()
    {
        var email = $"audit-profile-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password).ConfigureAwait(true);
        using var updateResponse = await client.PutAsJsonAsync(
            "/me/profile",
            new UpdateProfileRequest("Renamed User", "Marketing"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditEvent = await FindLatestAuditAsync(AuditActions.AuthProfileUpdated).ConfigureAwait(true);
        auditEvent.Action.Should().Be(AuditActions.AuthProfileUpdated);
        auditEvent.Metadata.Should().Contain("changedFields");
    }

    [Fact]
    public async Task Password_change_should_write_auth_password_changed_audit_event()
    {
        var email = $"audit-password-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor).ConfigureAwait(true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, SeedCredentials.Password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var authedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        authedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        foreach (var cookieHeader in loginResponse.Headers.GetValues("Set-Cookie"))
        {
            authedClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var changeResponse = await authedClient.PutAsJsonAsync(
            "/me/password",
            new ChangePasswordRequest(SeedCredentials.Password, "NewPassword1!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditEvent = await FindLatestAuditAsync(AuditActions.AuthPasswordChanged).ConfigureAwait(true);
        auditEvent.Metadata.Should().NotContain("NewPassword1!");
        auditEvent.Metadata.Should().NotContain(SeedCredentials.Password);
    }

    [Fact]
    public async Task Audit_list_should_filter_by_requestId_metadata()
    {
        var email = $"audit-filter-{Guid.NewGuid():N}@test.local";
        await CreateUserAsync(email, SeedCredentials.Password, Roles.Requestor, "Engineering").ConfigureAwait(true);

        using var requestorClient = await CreateAuthenticatedClientAsync(email, SeedCredentials.Password)
            .ConfigureAwait(true);
        using var createResponse = await requestorClient
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var adminClient = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);
        using var auditResponse = await adminClient
            .GetAsync($"/audit?requestId={created!.Id}&page=1&pageSize=20", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await auditResponse.Content
            .ReadFromJsonAsync<PagedResult<AuditEventDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(item =>
            item.Metadata != null && item.Metadata.ContainsKey("requestId"));
    }

    private static RequestMutationBody CreateMutationBody() =>
        new(
            Title: "Audit write test request",
            Department: null,
            SponsorshipTypeId: 1,
            EventName: "Audit Event",
            EventDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount: 1500m,
            Purpose: "Verify audit writes.",
            ExpectedBenefit: null,
            Remarks: null);

    private static async Task<long> CreateDraftRequestAsync(HttpClient client)
    {
        using var createResponse = await client
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        return created!.Id;
    }

    private static Task<HttpResponseMessage> UploadPdfAsync(
        HttpClient client,
        long requestId,
        byte[] fileBytes,
        string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", fileName);

        return client.PostAsync(
            $"/requests/{requestId}/attachments",
            content,
            TestContext.Current.CancellationToken);
    }

    private async Task<AuditEvent> FindLatestAuditAsync(string action)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.AuditEvents
            .AsNoTracking()
            .Where(entry => entry.Action == action)
            .OrderByDescending(entry => entry.Id)
            .FirstAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

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

        public const string Password = "Password1!";
    }
}
