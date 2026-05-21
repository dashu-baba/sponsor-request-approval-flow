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
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/health", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
