using System.Net;
using System.Text.Json;
using FluentAssertions;
using SponsorshipApproval.Api.Endpoints;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;

namespace SponsorshipApproval.Api.IntegrationTests;

public sealed class HealthEndpointTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task Health_live_should_return_self_component_without_dependencies()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/health/live", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await DeserializeAsync(response).ConfigureAwait(true);
        body.Status.Should().Be("Healthy");
        body.Components.Should().ContainKey("self");
        body.Components.Should().NotContainKey("postgres");
        body.Components.Should().NotContainKey("minio");
    }

    [Fact]
    public async Task Health_ready_should_return_postgres_and_minio_components()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/health/ready", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await DeserializeAsync(response).ConfigureAwait(true);
        body.Status.Should().Be("Healthy");
        body.Components.Should().ContainKey("postgres");
        body.Components.Should().ContainKey("minio");
        body.Components["postgres"].Status.Should().Be("Healthy");
        body.Components["minio"].Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_alias_should_match_ready_probe()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/health", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await DeserializeAsync(response).ConfigureAwait(true);
        body.Components.Should().ContainKeys("postgres", "minio");
    }

    private static async Task<HealthReportResponse> DeserializeAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content
            .ReadAsStreamAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var body = await JsonSerializer.DeserializeAsync<HealthReportResponse>(
            stream,
            JsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        body.Should().NotBeNull();
        return body!;
    }
}
