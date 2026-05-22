namespace SponsorshipApproval.Application.Common.Storage;

public interface IObjectStorage
{
    Task EnsureBucketExistsAsync(CancellationToken cancellationToken);

    Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task<ObjectStorageObject> GetAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed class ObjectStorageObject(Stream content, string contentType, long contentLength) : IDisposable
{
    public Stream Content { get; } = content;

    public string ContentType { get; } = contentType;

    public long ContentLength { get; } = contentLength;

    public void Dispose() => Content.Dispose();
}
