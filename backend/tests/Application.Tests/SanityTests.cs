using FluentAssertions;

namespace SponsorshipApproval.Application.Tests;

/// <summary>
/// Sanity tests: verifies xUnit + FluentAssertions are wired correctly in this project.
/// No application logic is tested here — that comes in later tasks.
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
