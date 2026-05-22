using FluentAssertions;
using FluentValidation;
using SponsorshipApproval.Application.Attachments;

namespace SponsorshipApproval.Application.Tests.Attachments;

public sealed class AttachmentFileValidatorTests
{
    [Fact]
    public void ValidateAndResolveExtension_should_accept_allowed_pdf_metadata()
    {
        var extension = AttachmentFileValidator.ValidateAndResolveExtension(
            "supporting-doc.pdf",
            "application/pdf",
            1024);

        extension.Should().Be(".pdf");
    }

    [Fact]
    public void ValidateAndResolveExtension_should_reject_oversize_files()
    {
        var action = () => AttachmentFileValidator.ValidateAndResolveExtension(
            "large.pdf",
            "application/pdf",
            AttachmentValidationConstants.MaxSizeBytes + 1);

        action.Should().Throw<ValidationException>()
            .Which.Errors.Should().Contain(error => error.PropertyName == "File");
    }

    [Fact]
    public void ValidateAndResolveExtension_should_reject_disallowed_content_type()
    {
        var action = () => AttachmentFileValidator.ValidateAndResolveExtension(
            "script.exe",
            "application/octet-stream",
            100);

        action.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateAndResolveExtension_should_reject_mismatched_extension()
    {
        var action = () => AttachmentFileValidator.ValidateAndResolveExtension(
            "photo.png",
            "application/pdf",
            100);

        action.Should().Throw<ValidationException>();
    }

    [Fact]
    public async Task ValidateContentSignatureAsync_should_accept_pdf_magic_bytes()
    {
        await using var stream = new MemoryStream("%PDF-1.4 test content"u8.ToArray());

        var action = async () => await AttachmentFileValidator
            .ValidateContentSignatureAsync(stream, "application/pdf", CancellationToken.None)
            .ConfigureAwait(true);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateContentSignatureAsync_should_reject_mismatched_magic_bytes()
    {
        await using var stream = new MemoryStream([0x00, 0x01, 0x02, 0x03, 0x04]);

        var action = async () => await AttachmentFileValidator
            .ValidateContentSignatureAsync(stream, "application/pdf", CancellationToken.None)
            .ConfigureAwait(true);

        await action.Should().ThrowAsync<ValidationException>();
    }
}
