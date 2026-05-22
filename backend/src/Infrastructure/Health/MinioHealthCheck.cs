using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SponsorshipApproval.Application.Common.Storage;

namespace SponsorshipApproval.Infrastructure.Health;

internal sealed class MinioHealthCheck(IAmazonS3 s3Client, IOptions<MinioOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var bucketName = options.Value.BucketName;

        await s3Client
            .GetBucketLocationAsync(bucketName, cancellationToken)
            .ConfigureAwait(false);

        return HealthCheckResult.Healthy($"MinIO bucket '{bucketName}' is reachable.");
    }
}
