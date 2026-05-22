using Microsoft.AspNetCore.Hosting;
using Testcontainers.Minio;

namespace SponsorshipApproval.Api.IntegrationTests.Infrastructure;

internal static class TestConfiguration
{
    internal const string JwtIssuer = "sponsorship-approval-tests";

    internal const string JwtAudience = "sponsorship-approval-api-tests";

    internal const string JwtSigningKey = "test-jwt-signing-key-at-least-32-characters-long";

    internal static void ApplyJwtSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenLifetimeDays", "7");
    }

    internal static void ApplyMinioSettings(IWebHostBuilder builder, MinioContainer minioContainer)
    {
        builder.UseSetting("Minio:Endpoint", minioContainer.GetConnectionString());
        builder.UseSetting("Minio:AccessKey", minioContainer.GetAccessKey());
        builder.UseSetting("Minio:SecretKey", minioContainer.GetSecretKey());
        builder.UseSetting("Minio:BucketName", PostgresWebApplicationFactory.MinioBucketName);
    }
}
