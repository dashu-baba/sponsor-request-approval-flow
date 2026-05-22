using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Api.IntegrationTests.Users;

public sealed class UserAdminTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task SystemAdmin_should_list_all_users()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .GetAsync("/users", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<UserSummaryResponse>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterOrEqualTo(4);
        users.Should().Contain(user => user.Email == SeedCredentials.RequestorEmail && user.Role == Roles.Requestor);
        users.Should().OnlyContain(user => !string.IsNullOrWhiteSpace(user.Role));
        users.Select(user => user.Email).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task SystemAdmin_should_create_user_that_can_log_in_and_load_profile()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var email = $"created-user-{Guid.NewGuid():N}@test.local";
        var body = new CreateUserRequest(
            email,
            "Created User",
            "Marketing",
            Roles.Manager,
            "Password1!");

        using var createResponse = await client
            .PostAsJsonAsync("/users", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location?.ToString().Should().StartWith("/users/");

        var created = await createResponse.Content
            .ReadFromJsonAsync<UserSummaryResponse>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        created!.Email.Should().Be(email);
        created.DisplayName.Should().Be("Created User");
        created.Department.Should().Be("Marketing");
        created.Role.Should().Be(Roles.Manager);

        using var listResponse = await client
            .GetAsync("/users", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        listResponse.EnsureSuccessStatusCode();
        var users = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<UserSummaryResponse>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        users.Should().Contain(user => user.Email == email && user.Role == Roles.Manager);

        using var newUserClient = await CreateAuthenticatedClientAsync(email, "Password1!").ConfigureAwait(true);
        using var meResponse = await newUserClient
            .GetAsync("/me", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await meResponse.Content
            .ReadFromJsonAsync<UserProfileResponse>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        profile!.Email.Should().Be(email);
        profile.DisplayName.Should().Be("Created User");
        profile.Department.Should().Be("Marketing");
        profile.Role.Should().Be(Roles.Manager);
    }

    [Fact]
    public async Task Duplicate_email_should_return_409()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var body = new CreateUserRequest(
            SeedCredentials.RequestorEmail,
            "Duplicate User",
            null,
            Roles.Requestor,
            "Password1!");

        using var response = await client
            .PostAsJsonAsync("/users", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_role_should_return_400()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var body = new CreateUserRequest(
            $"invalid-role-{Guid.NewGuid():N}@test.local",
            "Invalid Role User",
            null,
            "NotARealRole",
            "Password1!");

        using var response = await client
            .PostAsJsonAsync("/users", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Weak_initial_password_should_return_400()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var body = new CreateUserRequest(
            $"weak-password-{Guid.NewGuid():N}@test.local",
            "Weak Password User",
            null,
            Roles.Requestor,
            "short");

        using var response = await client
            .PostAsJsonAsync("/users", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(SeedCredentials.RequestorEmail, SeedCredentials.Password)]
    [InlineData(SeedCredentials.ManagerEmail, SeedCredentials.Password)]
    [InlineData(SeedCredentials.FinanceEmail, SeedCredentials.Password)]
    public async Task Non_system_admin_should_be_forbidden(string email, string password)
    {
        using var client = await CreateAuthenticatedClientAsync(email, password).ConfigureAwait(true);

        using var listResponse = await client
            .GetAsync("/users", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var createResponse = await client
            .PostAsJsonAsync(
                "/users",
                new CreateUserRequest(
                    $"forbidden-{Guid.NewGuid():N}@test.local",
                    "Forbidden User",
                    null,
                    Roles.Requestor,
                    "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_user_should_be_unauthorized()
    {
        using var client = factory.CreateClient();

        using var listResponse = await client
            .GetAsync("/users", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var createResponse = await client
            .PostAsJsonAsync(
                "/users",
                new CreateUserRequest(
                    $"anonymous-{Guid.NewGuid():N}@test.local",
                    "Anonymous User",
                    null,
                    Roles.Requestor,
                    "Password1!"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        listResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static class SeedCredentials
    {
        public const string AdminEmail = "admin@demo.local";

        public const string RequestorEmail = "requestor@demo.local";

        public const string ManagerEmail = "manager@demo.local";

        public const string FinanceEmail = "finance@demo.local";

        public const string Password = "Password1!";
    }
}
