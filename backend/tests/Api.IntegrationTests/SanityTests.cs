using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace SponsorshipApproval.Api.IntegrationTests;

/// <summary>
/// API integration tests backed by the in-memory test server.
/// </summary>
public sealed class SanityTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_should_return_success()
    {
        PostgreSqlContainer? postgres = null;

        try
        {
            postgres = new PostgreSqlBuilder("postgres:17.9-alpine3.23")
                .WithDatabase("sponsorship_approval_tests")
                .WithUsername("sponsorship_app")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilExternalTcpPortIsAvailable(5432)
                        .UntilCommandIsCompleted("pg_isready -U sponsorship_app -d sponsorship_approval_tests"))
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        catch (DockerUnavailableException exception)
        {
            Assert.Skip($"Docker is unavailable for Testcontainers: {exception.Message}");
        }

        const string connectionStringKey = "ConnectionStrings__Default";
        var previousConnectionString = Environment.GetEnvironmentVariable(connectionStringKey);

        try
        {
            Environment.SetEnvironmentVariable(connectionStringKey, postgres!.GetConnectionString());

            using var client = factory.CreateClient();

            using var response = await client
                .GetAsync("/health", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.IsSuccessStatusCode.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(connectionStringKey, previousConnectionString);

            if (postgres is not null)
            {
                await postgres.DisposeAsync().ConfigureAwait(true);
            }
        }
    }
}
