using FluentValidation;
using FluentValidation.Results;

namespace SponsorshipApproval.Application.Attachments;

public static class AttachmentFileValidator
{
    private const int SignatureLength = 12;

    public static string ValidateAndResolveExtension(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw CreateValidationException("File is empty.");
        }

        if (sizeBytes > AttachmentValidationConstants.MaxSizeBytes)
        {
            throw CreateValidationException(
                $"File exceeds the maximum size of {AttachmentValidationConstants.MaxSizeBytes / (1024 * 1024)} MB.");
        }

        if (!AttachmentValidationConstants.AllowedContentTypes.TryGetValue(contentType, out var allowedExtensions))
        {
            throw CreateValidationException("File type is not allowed.");
        }

        var extension = Path.GetExtension(Path.GetFileName(fileName));
        if (string.IsNullOrWhiteSpace(extension)
            || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw CreateValidationException("File extension is not allowed for the declared content type.");
        }

        return extension.ToLowerInvariant();
    }

    public static async Task ValidateContentSignatureAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw CreateValidationException("Uploaded file stream must be seekable.");
        }

        var originalPosition = content.Position;
        var header = new byte[SignatureLength];
        var bytesRead = await content.ReadAsync(header.AsMemory(0, SignatureLength), cancellationToken)
            .ConfigureAwait(false);
        content.Position = originalPosition;

        if (bytesRead < 4)
        {
            throw CreateValidationException("File content is too short or invalid.");
        }

        if (!IsSignatureValid(contentType, header))
        {
            throw CreateValidationException("File content does not match the declared content type.");
        }
    }

    private static bool IsSignatureValid(string contentType, ReadOnlySpan<byte> header) =>
        contentType.ToLowerInvariant() switch
        {
            "application/pdf" => StartsWithBytes(header, "%PDF"u8),
            "application/msword" => StartsWithBytes(header, [0xD0, 0xCF, 0x11, 0xE0]),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                StartsWithBytes(header, [0x50, 0x4B, 0x03, 0x04]),
            "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => StartsWithBytes(header, [0x89, 0x50, 0x4E, 0x47]),
            "image/gif" => StartsWithBytes(header, "GIF87a"u8) || StartsWithBytes(header, "GIF89a"u8),
            "image/webp" => header.Length >= 12
                && StartsWithBytes(header, "RIFF"u8)
                && header[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };

    private static bool StartsWithBytes(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix) =>
        value.Length >= prefix.Length && value[..prefix.Length].SequenceEqual(prefix);

    private static ValidationException CreateValidationException(string message) =>
        new([new ValidationFailure("File", message)]);
}
