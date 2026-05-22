namespace SponsorshipApproval.Application.Attachments.Models;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed record AttachmentDownloadResult(
    Stream Content,
    string ContentType,
    string FileName,
    long ContentLength);
