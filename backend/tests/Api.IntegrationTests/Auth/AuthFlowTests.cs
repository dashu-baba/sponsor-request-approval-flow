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

namespace SponsorshipApproval.Api.IntegrationTests.Auth;

public sealed class AuthFlowTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task Login_refresh_me_and_logout_should_succeed_for_requestor()
    {
        await CreateUserAsync("requestor@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

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
        AssertRefreshCookieSecurity(cookieHeaders);

        using var authedClient = factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);

        using var meResponse = await authedClient
            .GetAsync("/me", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        profile!.Role.Should().Be(Roles.Requestor);

        using var refreshClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
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
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/me", TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_without_cookie_should_return_401()
    {
        using var client = factory.CreateClient();
        using var response = await client
            .PostAsync("/auth/refresh", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_revoked_token_should_return_401()
    {
        await CreateUserAsync("refresh-revoke@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var loginResponse = await loginClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("refresh-revoke@test.local", "Password1!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        loginResponse.EnsureSuccessStatusCode();

        loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders).Should().BeTrue();
        var originalCookieHeaders = setCookieHeaders!.ToArray();

        using var refreshClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        foreach (var cookieHeader in originalCookieHeaders)
        {
            refreshClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var firstRefreshResponse = await refreshClient
            .PostAsync("/auth/refresh", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var revokedRefreshClient = factory.CreateClient();
        foreach (var cookieHeader in originalCookieHeaders)
        {
            revokedRefreshClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var revokedRefreshResponse = await revokedRefreshClient
            .PostAsync("/auth/refresh", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        revokedRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("known@test.local", "WrongPassword1!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void AssertRefreshCookieSecurity(string[] cookieHeaders)
    {
        var cookie = cookieHeaders.Single(header => header.Contains("refresh_token", StringComparison.Ordinal));
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=strict");
        cookie.Should().ContainEquivalentOf("path=/api/auth");
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

    private async Task CreateUserAsync(string email, string password, string role)
    {
        using var scope = factory.Services.CreateScope();
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
