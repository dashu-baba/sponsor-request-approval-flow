using System.ComponentModel.DataAnnotations;

namespace SponsorshipApproval.Application.Common.Storage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string BucketName { get; init; } = string.Empty;
}
