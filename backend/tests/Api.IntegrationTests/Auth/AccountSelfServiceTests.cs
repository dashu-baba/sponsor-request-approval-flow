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
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Api.IntegrationTests.Auth;

public sealed class AccountSelfServiceTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task Update_profile_should_persist_display_name_and_department()
    {
        await CreateUserAsync("profile-user@test.local", "Password1!", Roles.Requestor, "Original Dept").ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("profile-user@test.local", "Password1!")
            .ConfigureAwait(true);

        using var updateResponse = await client.PutAsJsonAsync(
            "/me/profile",
            new UpdateProfileRequest("Updated Name", "New Department"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UserProfileResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        updated!.DisplayName.Should().Be("Updated Name");
        updated.Department.Should().Be("New Department");
        updated.Email.Should().Be("profile-user@test.local");

        using var meResponse = await client.GetAsync("/me", TestContext.Current.CancellationToken).ConfigureAwait(true);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        profile!.DisplayName.Should().Be("Updated Name");
        profile.Department.Should().Be("New Department");
    }

    [Fact]
    public async Task Me_endpoints_without_token_should_return_401()
    {
        using var client = factory.CreateClient();

        using var meResponse = await client.GetAsync("/me", TestContext.Current.CancellationToken).ConfigureAwait(true);
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var profileResponse = await client.PutAsJsonAsync(
            "/me/profile",
            new UpdateProfileRequest("Name", null),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var passwordResponse = await client.PutAsJsonAsync(
            "/me/password",
            new ChangePasswordRequest("Password1!", "Password2!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        passwordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_with_wrong_current_password_should_return_400()
    {
        await CreateUserAsync("wrong-pass@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("wrong-pass@test.local", "Password1!")
            .ConfigureAwait(true);

        using var response = await client.PutAsJsonAsync(
            "/me/password",
            new ChangePasswordRequest("WrongPassword1!", "Password2!"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        problem!.Detail.Should().Contain("Current password is incorrect");
    }

    [Fact]
    public async Task Change_password_with_weak_new_password_should_return_400()
    {
        await CreateUserAsync("weak-pass@test.local", "Password1!", Roles.Requestor).ConfigureAwait(true);

        using var client = await CreateAuthenticatedClientAsync("weak-pass@test.local", "Password1!")
            .ConfigureAwait(true);

        using var response = await client.PutAsJsonAsync(
            "/me/password",
            new ChangePasswordRequest("Password1!", "short"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Change_password_should_invalidate_old_tokens_and_keep_current_session()
    {
        const string email = "password-change@test.local";
        const string originalPassword = "Password1!";
        const string newPassword = "Password2!";

        await CreateUserAsync(email, originalPassword, Roles.Requestor).ConfigureAwait(true);

        using var deviceAClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var deviceALogin = await deviceAClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, originalPassword),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        deviceALogin.EnsureSuccessStatusCode();
        var deviceALoginBody = await deviceALogin.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        deviceALogin.Headers.TryGetValues("Set-Cookie", out var deviceACookies).Should().BeTrue();

        using var deviceBClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var deviceBLogin = await deviceBClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, originalPassword),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        deviceBLogin.EnsureSuccessStatusCode();
        deviceBLogin.Headers.TryGetValues("Set-Cookie", out var deviceBCookies).Should().BeTrue();

        var oldAccessToken = deviceALoginBody!.AccessToken;

        using var changePasswordClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        foreach (var cookieHeader in deviceACookies!)
        {
            changePasswordClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        changePasswordClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", oldAccessToken);

        using var changeResponse = await changePasswordClient.PutAsJsonAsync(
            "/me/password",
            new ChangePasswordRequest(originalPassword, newPassword),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newLoginBody = await changeResponse.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        newLoginBody!.AccessToken.Should().NotBe(oldAccessToken);
        changeResponse.Headers.TryGetValues("Set-Cookie", out var newDeviceACookies).Should().BeTrue();

        using var refreshedDeviceAClient = factory.CreateClient();
        refreshedDeviceAClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", newLoginBody.AccessToken);
        using var newMeResponse = await refreshedDeviceAClient
            .GetAsync("/me", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        newMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var staleTokenClient = factory.CreateClient();
        staleTokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldAccessToken);
        using var staleMeResponse = await staleTokenClient
            .GetAsync("/me", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        staleMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var revokedDeviceBClient = factory.CreateClient();
        foreach (var cookieHeader in deviceBCookies!)
        {
            revokedDeviceBClient.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        }

        using var revokedRefreshResponse = await revokedDeviceBClient
            .PostAsync("/auth/refresh", content: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        revokedRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _ = newDeviceACookies;
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
}
