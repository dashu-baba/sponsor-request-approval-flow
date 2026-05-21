using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

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
        const string connectionStringKey = "ConnectionStrings__Default";
        var previousConnectionString = Environment.GetEnvironmentVariable(connectionStringKey);

        try
        {
            Environment.SetEnvironmentVariable(
                connectionStringKey,
                "Host=localhost;Port=5432;Database=sponsorship_approval_tests;Username=sponsorship_app;Password=test-postgres-password");

            using var client = factory.CreateClient();

            using var response = await client
                .GetAsync("/health", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.IsSuccessStatusCode.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(connectionStringKey, previousConnectionString);
        }
    }
}
