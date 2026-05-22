namespace SponsorshipApproval.Application.Attachments;

public static class AttachmentObjectKeyGenerator
{
    public static string Generate(long requestId, string extension)
    {
        var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";

        return $"requests/{requestId}/{Guid.NewGuid():N}{normalizedExtension}";
    }
}
