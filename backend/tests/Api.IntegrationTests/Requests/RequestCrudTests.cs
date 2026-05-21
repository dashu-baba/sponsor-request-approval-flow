using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

public sealed class RequestCrudTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly Guid ConferenceTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

    private static readonly Guid CommunityEventTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

    private static readonly Guid SeededPendingManagerRequestId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

    [Fact]
    public async Task Create_list_get_and_update_draft_should_succeed_for_requestor()
    {
        await CreateUserAsync("crud-owner@test.local", "Password1!", Roles.Requestor, "Product")
            .ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("crud-owner@test.local", "Password1!")
            .ConfigureAwait(true);

        var createBody = CreateMutationBody(title: "New draft request", department: null);
        using var createResponse = await client
            .PostAsJsonAsync("/requests", createBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        created.Should().NotBeNull();
        created!.Status.Should().Be(RequestStatus.Draft);
        created.RequestorName.Should().Be("crud-owner");
        created.RequestorId.Should().NotBeNullOrWhiteSpace();
        created.Department.Should().Be("Product");

        using var listResponse = await client
            .GetAsync("/requests?page=1&pageSize=10", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content
            .ReadFromJsonAsync<PagedResult<RequestListItemDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        list!.Items.Should().Contain(item => item.Id == created.Id);

        using var getResponse = await client
            .GetAsync($"/requests/{created.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = createBody with
        {
            Title = "Updated draft title",
            RequestedAmount = 2200m,
        };

        using var updateResponse = await client
            .PutAsJsonAsync($"/requests/{created.Id}", updateBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        updated!.Title.Should().Be("Updated draft title");
        updated.RequestedAmount.Should().Be(2200m);
        updated.RequestorName.Should().Be("crud-owner");

        var typeChangeBody = updateBody with { SponsorshipTypeId = CommunityEventTypeId };

        using var typeChangeResponse = await client
            .PutAsJsonAsync($"/requests/{created.Id}", typeChangeBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        typeChangeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var typeChanged = await typeChangeResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        typeChanged!.SponsorshipTypeId.Should().Be(CommunityEventTypeId);
        typeChanged.SponsorshipTypeName.Should().Be("Community Event");
    }

    [Fact]
    public async Task Update_non_draft_request_should_return_409()
    {
        using var client = await CreateAuthenticatedClientAsync(
                SeedCredentials.RequestorEmail,
                SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .PutAsJsonAsync(
                $"/requests/{SeededPendingManagerRequestId}",
                CreateMutationBody(title: "Illegal edit"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cross_user_access_should_return_403()
    {
        await CreateUserAsync("owner-a@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);
        await CreateUserAsync("owner-b@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var ownerClient = await CreateAuthenticatedClientAsync("owner-a@test.local", "Password1!")
            .ConfigureAwait(true);

        using var createResponse = await ownerClient
            .PostAsJsonAsync("/requests", CreateMutationBody(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content
            .ReadFromJsonAsync<RequestDetailDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var otherClient = await CreateAuthenticatedClientAsync("owner-b@test.local", "Password1!")
            .ConfigureAwait(true);

        using var getResponse = await otherClient
            .GetAsync($"/requests/{created!.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var putResponse = await otherClient
            .PutAsJsonAsync(
                $"/requests/{created.Id}",
                CreateMutationBody(title: "Forbidden update"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Requests_without_token_should_return_401()
    {
        using var client = factory.CreateClient();
        using var response = await client
            .GetAsync("/requests", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_with_invalid_amount_should_return_400_problem_details()
    {
        await CreateUserAsync("invalid-amount@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("invalid-amount@test.local", "Password1!")
            .ConfigureAwait(true);

        using var response = await client
            .PostAsJsonAsync(
                "/requests",
                CreateMutationBody() with { RequestedAmount = 0m },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        problem!.Title.Should().Be("Validation failed");
    }

    private static RequestMutationBody CreateMutationBody(
        string title = "Integration test request",
        string? department = "Engineering") =>
        new(
            Title: title,
            Department: department,
            SponsorshipTypeId: ConferenceTypeId,
            EventName: "Future Event",
            EventDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            RequestedAmount: 1250m,
            Purpose: "Support a community outreach program.",
            ExpectedBenefit: "Increased brand awareness.",
            Remarks: "Created from integration test.");

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
    }
}
