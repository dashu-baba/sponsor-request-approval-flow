namespace SponsorshipApproval.Application.Attachments;

public static class AttachmentObjectKeyGenerator
{
    public static string Generate(Guid requestId, string extension)
    {
        var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";

        return $"requests/{requestId:N}/{Guid.NewGuid():N}{normalizedExtension}";
    }
}
