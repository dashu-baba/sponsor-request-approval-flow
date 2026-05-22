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
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Api.IntegrationTests.SponsorshipTypes;

public sealed class SponsorshipTypeAdminTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
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
    public async Task Non_admin_can_list_active_types_but_cannot_mutate()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.RequestorEmail, SeedCredentials.Password)
            .ConfigureAwait(true);
        var id = 999_999L;
        var body = new SponsorshipTypeMutationBody("Forbidden Type", "A requestor cannot manage types.");

        using var getResponse = await client
            .GetAsync("/sponsorship-types", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var postResponse = await client
            .PostAsJsonAsync("/sponsorship-types", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var putResponse = await client
            .PutAsJsonAsync($"/sponsorship-types/{id}", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var deleteResponse = await client
            .DeleteAsync($"/sponsorship-types/{id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await getResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<SponsorshipTypeDto>>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        list.Should().NotBeEmpty();
        list.Should().OnlyContain(type => type.IsActive);

        postResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
    public async Task Recreate_after_soft_delete_should_succeed_with_same_name()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);
        var name = $"Recreate Test Type {Guid.NewGuid():N}";
        var body = new SponsorshipTypeMutationBody(name, "Original record.");

        using var createResponse = await client
            .PostAsJsonAsync("/sponsorship-types", body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content
            .ReadFromJsonAsync<SponsorshipTypeDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var deleteResponse = await client
            .DeleteAsync($"/sponsorship-types/{created!.Id}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var recreateResponse = await client
            .PostAsJsonAsync(
                "/sponsorship-types",
                body with { Description = "Replacement active record." },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        recreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var recreated = await recreateResponse.Content
            .ReadFromJsonAsync<SponsorshipTypeDto>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        recreated!.Id.Should().NotBe(created.Id);
        recreated.Name.Should().Be(name);
        recreated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_referenced_type_should_soft_disable_and_preserve_requests()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);
        var (typeId, requestId) = await CreateReferencedTypeAsync().ConfigureAwait(true);

        using var response = await client
            .DeleteAsync($"/sponsorship-types/{typeId}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = await dbContext.SponsorshipTypes
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == typeId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var referencingRequest = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == requestId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        type.IsActive.Should().BeFalse();
        referencingRequest.Should().NotBeNull();
        referencingRequest!.SponsorshipTypeId.Should().Be(typeId);
    }

    [Fact]
    public async Task Update_and_delete_unknown_ids_should_return_404()
    {
        using var client = await CreateAuthenticatedClientAsync(SeedCredentials.AdminEmail, SeedCredentials.Password)
            .ConfigureAwait(true);
        var missingId = 999_999L;

        using var updateResponse = await client
            .PutAsJsonAsync(
                $"/sponsorship-types/{missingId}",
                new SponsorshipTypeMutationBody("Missing Type", "Missing."),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var deleteResponse = await client
            .DeleteAsync($"/sponsorship-types/{missingId}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(long TypeId, long RequestId)> CreateReferencedTypeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = new SponsorshipType
        {
            Name = $"Referenced Test Type {Guid.NewGuid():N}",
            Description = "Dedicated type for referenced-delete testing.",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = SeedUserIds.Admin,
        };
        dbContext.SponsorshipTypes.Add(type);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var request = new SponsorshipRequest
        {
            Title = "Referenced type test request",
            RequestorName = "Integration Requestor",
            RequestorId = SeedUserIds.Requestor,
            Department = "Engineering",
            SponsorshipTypeId = type.Id,
            EventName = "Referenced Type Event",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RequestedAmount = 1000m,
            Purpose = "Verify delete preserves referencing requests.",
            Status = RequestStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = SeedUserIds.Requestor,
        };
        dbContext.SponsorshipRequests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        return (type.Id, request.Id);
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

    private static class SeedUserIds
    {
        public const string Admin = "seed-admin";

        public const string Requestor = "seed-requestor";
    }
}
