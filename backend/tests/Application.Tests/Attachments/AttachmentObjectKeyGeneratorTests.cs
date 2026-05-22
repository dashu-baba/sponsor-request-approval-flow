using FluentAssertions;
using SponsorshipApproval.Application.Attachments;

namespace SponsorshipApproval.Application.Tests.Attachments;

public sealed class AttachmentObjectKeyGeneratorTests
{
    [Fact]
    public void Generate_should_include_request_id_and_extension_without_path_traversal()
    {
        var requestId = 42L;

        var key = AttachmentObjectKeyGenerator.Generate(requestId, ".pdf");

        key.Should().StartWith("requests/42/");
        key.Should().EndWith(".pdf");
        key.Should().NotContain("..");
    }

    [Fact]
    public void Generate_should_normalize_extension_without_leading_dot()
    {
        var requestId = 7L;

        var key = AttachmentObjectKeyGenerator.Generate(requestId, "png");

        key.Should().EndWith(".png");
    }
}
