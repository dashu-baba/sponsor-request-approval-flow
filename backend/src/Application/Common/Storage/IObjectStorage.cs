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

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed class ObjectStorageObject : IDisposable
{
    private readonly IDisposable? _ownedResource;

    public ObjectStorageObject(
        Stream content,
        string contentType,
        long contentLength,
        IDisposable? ownedResource = null)
    {
        Content = content;
        ContentType = contentType;
        ContentLength = contentLength;
        _ownedResource = ownedResource;
    }

    public Stream Content { get; }

    public string ContentType { get; }

    public long ContentLength { get; }

    public void Dispose() => _ownedResource?.Dispose();
}
