using SponsorshipApproval.Application.Common.Storage;

namespace SponsorshipApproval.Application.Attachments.Models;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed class AttachmentDownloadResult(ObjectStorageObject storageObject, string contentType, string fileName)
    : IDisposable
{
    public Stream Content { get; } = storageObject.Content;

    public string ContentType { get; } = contentType;

    public string FileName { get; } = fileName;

    public long ContentLength { get; } = storageObject.ContentLength;

    public void Dispose() => storageObject.Dispose();
}
