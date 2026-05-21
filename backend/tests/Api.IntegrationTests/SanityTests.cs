using FluentAssertions;

namespace SponsorshipApproval.Api.IntegrationTests;

/// <summary>
/// Sanity tests: verifies xUnit + FluentAssertions are wired correctly in this project.
/// Real integration tests (WebApplicationFactory + Testcontainers) are added in later tasks.
/// </summary>
public class SanityTests
{
    [Fact]
    public void True_should_be_true()
    {
        // Arrange / Act
        var value = true;

        // Assert
        value.Should().BeTrue();
    }
}
