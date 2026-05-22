using Microsoft.Extensions.Diagnostics.HealthChecks;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Health;

internal sealed class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database
            .CanConnectAsync(cancellationToken)
            .ConfigureAwait(false);

        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL connection succeeded.")
            : HealthCheckResult.Unhealthy("PostgreSQL connection failed.");
    }
}
