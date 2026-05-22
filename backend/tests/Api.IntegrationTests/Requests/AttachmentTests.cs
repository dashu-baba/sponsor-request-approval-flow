using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Attachments;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Api.IntegrationTests.Requests;

public sealed class AttachmentTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly Guid ConferenceTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

    private static readonly byte[] MinimalPdfBytes = "%PDF-1.4\n%EOF\n"u8.ToArray();

    [Fact]
    public async Task Owner_upload_list_and_download_should_succeed()
    {
        await CreateUserAsync("attachment-owner@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("attachment-owner@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(client, requestId, MinimalPdfBytes, "supporting-doc.pdf")
            .ConfigureAwait(true);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploaded = await uploadResponse.Content
            .ReadFromJsonAsync<AttachmentDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        uploaded.Should().NotBeNull();
        uploaded!.FileName.Should().Be("supporting-doc.pdf");
        uploaded.ContentType.Should().Be("application/pdf");
        uploaded.SizeBytes.Should().Be(MinimalPdfBytes.Length);

        using var listResponse = await client
            .GetAsync($"/requests/{requestId}/attachments", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attachments = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<AttachmentDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        attachments.Should().ContainSingle(item => item.Id == uploaded.Id);

        using var downloadResponse = await client
            .GetAsync($"/requests/{requestId}/attachments/{uploaded.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var downloadedBytes = await downloadResponse.Content
            .ReadAsByteArrayAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        downloadedBytes.Should().Equal(MinimalPdfBytes);
    }

    [Fact]
    public async Task Oversize_upload_should_be_rejected()
    {
        await CreateUserAsync("attachment-oversize@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("attachment-oversize@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);
        var oversizeBytes = new byte[AttachmentValidationConstants.MaxSizeBytes + 1];
        MinimalPdfBytes.CopyTo(oversizeBytes, 0);

        using var uploadResponse = await UploadPdfAsync(client, requestId, oversizeBytes, "oversize.pdf")
            .ConfigureAwait(true);

        uploadResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Non_owner_upload_should_return_403()
    {
        await CreateUserAsync("attachment-owner-a@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);
        await CreateUserAsync("attachment-owner-b@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var ownerClient = await CreateAuthenticatedClientAsync("attachment-owner-a@test.local", "Password1!")
            .ConfigureAwait(true);
        using var otherClient = await CreateAuthenticatedClientAsync("attachment-owner-b@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(ownerClient).ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(otherClient, requestId, MinimalPdfBytes, "forbidden.pdf")
            .ConfigureAwait(true);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Non_owner_list_and_download_should_return_403()
    {
        await CreateUserAsync("attachment-list-owner-a@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);
        await CreateUserAsync("attachment-list-owner-b@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var ownerClient = await CreateAuthenticatedClientAsync("attachment-list-owner-a@test.local", "Password1!")
            .ConfigureAwait(true);
        using var otherClient = await CreateAuthenticatedClientAsync("attachment-list-owner-b@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(ownerClient).ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(ownerClient, requestId, MinimalPdfBytes, "supporting-doc.pdf")
            .ConfigureAwait(true);
        uploadResponse.EnsureSuccessStatusCode();

        var uploaded = await uploadResponse.Content
            .ReadFromJsonAsync<AttachmentDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var listResponse = await otherClient
            .GetAsync($"/requests/{requestId}/attachments", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var downloadResponse = await otherClient
            .GetAsync($"/requests/{requestId}/attachments/{uploaded!.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Upload_to_non_draft_request_should_return_409()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.RequestorEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(
                client,
                SeedCredentials.PendingManagerRequestId,
                MinimalPdfBytes,
                "late-upload.pdf")
            .ConfigureAwait(true);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Mismatched_magic_bytes_should_return_400_problem_details()
    {
        await CreateUserAsync("attachment-magic@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("attachment-magic@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);

        using var uploadResponse = await UploadPdfAsync(
                client,
                requestId,
                [0x00, 0x01, 0x02, 0x03, 0x04],
                "fake.pdf")
            .ConfigureAwait(true);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await uploadResponse.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        problem!.Title.Should().Be("Validation failed");
    }

    [Fact]
    public async Task Missing_file_field_should_return_400()
    {
        await CreateUserAsync("attachment-missing-file@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("attachment-missing-file@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);

        using var response = await client
            .PostAsync(
                $"/requests/{requestId}/attachments",
                new MultipartFormDataContent(),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_content_type_should_return_400_problem_details()
    {
        await CreateUserAsync("attachment-invalid@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("attachment-invalid@test.local", "Password1!")
            .ConfigureAwait(true);

        var requestId = await CreateDraftRequestAsync(client).ConfigureAwait(true);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(MinimalPdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "supporting-doc.pdf");

        using var response = await client
            .PostAsync($"/requests/{requestId}/attachments", content, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        problem!.Title.Should().Be("Validation failed");
    }

    private static async Task<Guid> CreateDraftRequestAsync(HttpClient client)
    {
        var body = new RequestMutationBody(
            Title: "Attachment test request",
            Department: "Engineering",
            SponsorshipTypeId: ConferenceTypeId,
            EventName: "Future Event",
            EventDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount: 1500m,
            Purpose: "Attachment integration coverage.",
            ExpectedBenefit: null,
            Remarks: null);

        using var createResponse = await client
            .PostAsJsonAsync("/requests", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        created!.Status.Should().Be(RequestStatus.Draft);
        return created.Id;
    }

    private static Task<HttpResponseMessage> UploadPdfAsync(
        HttpClient client,
        Guid requestId,
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

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
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

    private static class SeedCredentials
    {
        public const string RequestorEmail = "requestor@demo.local";

        public const string Password = "Password1!";

        public static readonly Guid PendingManagerRequestId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    }
}
