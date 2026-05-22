namespace SponsorshipApproval.Application.Attachments;

public static class AttachmentValidationConstants
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string[]> AllowedContentTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = [".pdf"],
            ["application/msword"] = [".doc"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/gif"] = [".gif"],
            ["image/webp"] = [".webp"],
        };
}
