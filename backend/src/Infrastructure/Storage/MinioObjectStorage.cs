using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SponsorshipApproval.Application.Common.Storage;

namespace SponsorshipApproval.Infrastructure.Storage;

public sealed class MinioObjectStorage(IAmazonS3 s3Client, IOptions<MinioOptions> options) : IObjectStorage
{
    private readonly MinioOptions _options = options.Value;

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await s3Client
                .GetBucketLocationAsync(_options.BucketName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await s3Client
                .PutBucketAsync(new PutBucketRequest { BucketName = _options.BucketName }, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await s3Client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjectStorageObject> GetAsync(string objectKey, CancellationToken cancellationToken)
    {
        var response = await s3Client
            .GetObjectAsync(_options.BucketName, objectKey, cancellationToken)
            .ConfigureAwait(false);

        var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
            ? "application/octet-stream"
            : response.Headers.ContentType;

        return new ObjectStorageObject(
            response.ResponseStream,
            contentType,
            response.ContentLength);
    }
}

public static class MinioServiceCollectionExtensions
{
    public static IServiceCollection AddMinioObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MinioOptions>()
            .Bind(configuration.GetSection(MinioOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MinioOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Minio endpoint is not configured.");
            }

            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
            };

            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<IObjectStorage, MinioObjectStorage>();

        return services;
    }
}
