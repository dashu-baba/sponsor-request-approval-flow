using System.Net;
using System.Text.Json;
using FluentAssertions;
using SponsorshipApproval.Api.Endpoints;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;

namespace SponsorshipApproval.Api.IntegrationTests;

public sealed class HealthEndpointUnhealthyTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task Health_ready_should_return_503_when_minio_is_unreachable()
    {
        using var client = factory.CreateClient();

        await factory.StopMinioContainerAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var response = await client
            .GetAsync("/health/ready", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var body = await DeserializeAsync(response).ConfigureAwait(true);
        body.Status.Should().Be("Unhealthy");
        body.Components["postgres"].Status.Should().Be("Healthy");
        body.Components["minio"].Status.Should().Be("Unhealthy");
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
