using FluentAssertions;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Domain.Tests.Requests;

public sealed class DomainModelTests
{
    [Fact]
    public void Sponsorship_request_should_start_as_draft()
    {
        var request = new SponsorshipRequest();

        request.Status.Should().Be(RequestStatus.Draft);
    }

    [Fact]
    public void Domain_project_should_not_reference_entity_framework()
    {
        var references = typeof(SponsorshipRequest).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        references.Should().NotContain(reference => reference != null && reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
