using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SponsorshipApproval.Infrastructure.Health;

public static class HealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("API process is running."),
                tags: [HealthCheckTags.Live])
            .AddCheck<PostgresHealthCheck>("postgres", tags: [HealthCheckTags.Ready])
            .AddCheck<MinioHealthCheck>("minio", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
