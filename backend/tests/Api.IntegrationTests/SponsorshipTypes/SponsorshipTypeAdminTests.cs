using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.SponsorshipTypes;

public sealed class SponsorshipTypeAdminTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly Guid ReferencedConferenceTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

    [Fact]
    public async Task Admin_can_create_list_update_and_delete_sponsorship_type()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var createBody = new SponsorshipTypeMutationBody(
            Name: $"Admin Test Type {Guid.NewGuid():N}",
            Description: "Created from integration test.");

        using var createResponse = await client
            .PostAsJsonAsync("/sponsorship-types", createBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content
            .ReadFromJsonAsync<SponsorshipTypeDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        created.Should().NotBeNull();
        created!.Name.Should().Be(createBody.Name);
        created.Description.Should().Be(createBody.Description);
        created.IsActive.Should().BeTrue();

        using var listResponse = await client
            .GetAsync("/sponsorship-types", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<SponsorshipTypeDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        list.Should().Contain(type => type.Id == created.Id && type.IsActive);

        var updateBody = createBody with
        {
            Name = $"{createBody.Name} Updated",
            Description = "Updated from integration test.",
        };

        using var updateResponse = await client
            .PutAsJsonAsync($"/sponsorship-types/{created.Id}", updateBody, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<SponsorshipTypeDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        updated!.Name.Should().Be(updateBody.Name);
        updated.Description.Should().Be(updateBody.Description);
        updated.UpdatedAt.Should().NotBeNull();

        using var deleteResponse = await client
            .DeleteAsync($"/sponsorship-types/{created.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deleted = await dbContext.SponsorshipTypes
            .AsNoTracking()
            .SingleAsync(type => type.Id == created.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        deleted.IsActive.Should().BeFalse();
        deleted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Non_admin_should_be_forbidden()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.RequestorEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .GetAsync("/sponsorship-types", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_user_should_be_unauthorized()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/sponsorship-types", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Duplicate_active_name_should_return_409()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        var name = $"Duplicate Test Type {Guid.NewGuid():N}";
        using var createResponse = await client
            .PostAsJsonAsync(
                "/sponsorship-types",
                new SponsorshipTypeMutationBody(name, "Original active record."),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();

        using var response = await client
            .PostAsJsonAsync(
                "/sponsorship-types",
                new SponsorshipTypeMutationBody(name.ToLowerInvariant(), "Duplicate with different casing."),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_referenced_type_should_soft_disable_and_preserve_requests()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);

        using var response = await client
            .DeleteAsync($"/sponsorship-types/{ReferencedConferenceTypeId}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = await dbContext.SponsorshipTypes
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == ReferencedConferenceTypeId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var referencingRequests = await dbContext.SponsorshipRequests
            .CountAsync(
                request => request.SponsorshipTypeId == ReferencedConferenceTypeId,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        type.IsActive.Should().BeFalse();
        referencingRequests.Should().BeGreaterThan(0);
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

        public const string Password = "Password1!";
    }
}
