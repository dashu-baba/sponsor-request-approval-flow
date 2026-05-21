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
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.Auth;

public sealed class AuthFlowTests : IAsyncLifetime
{
    private PostgresWebApplicationFactory? _factory;

    public async ValueTask InitializeAsync()
    {
        _factory = new PostgresWebApplicationFactory();
        try
        {
            await _factory.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("Docker is unavailable", StringComparison.Ordinal))
        {
            Assert.Skip(exception.Message);
        }

        await SeedRolesAsync().ConfigureAwait(true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Login_refresh_me_and_logout_should_succeed_for_requestor()
    {
        await CreateUserAsync("requestor@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("requestor@test.local", "Password1!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginBody.Should().NotBeNull();
        loginBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders).Should().BeTrue();
        var cookieHeaders = setCookieHeaders!.ToArray();
        cookieHeaders.Any(header => header.Contains("refresh_token", StringComparison.Ordinal)).Should().BeTrue();

        using var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);

        using var meResponse = await authedClient
            .GetAsync("/me", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        profile!.Role.Should().Be(Roles.Requestor);

        using var refreshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        foreach (var cookieHeader in cookieHeaders)
        {
            refreshClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var refreshResponse = await refreshClient
            .PostAsync("/auth/refresh", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var logoutResponse = await refreshClient
            .PostAsync("/auth/logout", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Me_without_token_should_return_401()
    {
        using var client = _factory!.CreateClient();
        using var response = await client.GetAsync("/me", TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task System_ping_should_return_403_for_requestor_and_204_for_system_admin()
    {
        await CreateUserAsync("requestor2@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);
        await CreateUserAsync("admin@test.local", "Password1!", Roles.SystemAdmin).ConfigureAwait(true);

        using var requestorClient = await CreateAuthenticatedClientAsync("requestor2@test.local", "Password1!")
            .ConfigureAwait(true);
        using var requestorResponse = await requestorClient
            .GetAsync("/system/ping", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        requestorResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var adminClient = await CreateAuthenticatedClientAsync("admin@test.local", "Password1!")
            .ConfigureAwait(true);
        using var adminResponse = await adminClient
            .GetAsync("/system/ping", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_with_invalid_credentials_should_return_401()
    {
        await CreateUserAsync("known@test.local", "Password1!", Roles.Manager).ConfigureAwait(true);

        using var client = _factory!.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("known@test.local", "WrongPassword1!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        using var loginClient = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }

    private async Task SeedRolesAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(true))
            {
                await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(true);
            }
        }
    }

    private async Task CreateUserAsync(string email, string password, string role)
    {
        using var scope = _factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = email.Split('@')[0],
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, password).ConfigureAwait(true);
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role).ConfigureAwait(true);
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(error => error.Description)));
    }
}
