using FluentAssertions;
using SponsorshipApproval.Application.Attachments;

namespace SponsorshipApproval.Application.Tests.Attachments;

public sealed class AttachmentObjectKeyGeneratorTests
{
    [Fact]
    public void Generate_should_include_request_id_and_extension_without_path_traversal()
    {
        var requestId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var key = AttachmentObjectKeyGenerator.Generate(requestId, ".pdf");

        key.Should().StartWith("requests/11111111111111111111111111111111/");
        key.Should().EndWith(".pdf");
        key.Should().NotContain("..");
    }

    [Fact]
    public void Generate_should_normalize_extension_without_leading_dot()
    {
        var requestId = Guid.NewGuid();

        var key = AttachmentObjectKeyGenerator.Generate(requestId, "png");

        key.Should().EndWith(".png");
    }
}
