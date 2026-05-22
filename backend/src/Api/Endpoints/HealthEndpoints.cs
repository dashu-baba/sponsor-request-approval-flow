using Microsoft.Extensions.Diagnostics.HealthChecks;
using SponsorshipApproval.Infrastructure.Health;

namespace SponsorshipApproval.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var health = app.MapGroup("/health").WithTags("System");

        health.MapGet("/live", GetLiveAsync)
            .AllowAnonymous()
            .WithSummary("Liveness probe")
            .WithDescription("Returns 200 when the API process is running. Does not check dependencies.")
            .Produces<HealthReportResponse>(StatusCodes.Status200OK);

        health.MapGet("/ready", GetReadyAsync)
            .AllowAnonymous()
            .WithSummary("Readiness probe")
            .WithDescription("Returns 200 when PostgreSQL and MinIO are reachable; 503 when a dependency is down.")
            .Produces<HealthReportResponse>(StatusCodes.Status200OK)
            .Produces<HealthReportResponse>(StatusCodes.Status503ServiceUnavailable);

        health.MapGet("", GetReadyAsync)
            .AllowAnonymous()
            .WithSummary("Readiness probe (alias)")
            .WithDescription("Alias for GET /health/ready. Kept for Docker Compose and deploy smoke checks.")
            .Produces<HealthReportResponse>(StatusCodes.Status200OK)
            .Produces<HealthReportResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static Task<IResult> GetLiveAsync(
        HealthCheckService healthChecks,
        CancellationToken cancellationToken) =>
        WriteHealthReportAsync(
            healthChecks,
            registration => registration.Tags.Contains(HealthCheckTags.Live),
            cancellationToken);

    private static Task<IResult> GetReadyAsync(
        HealthCheckService healthChecks,
        CancellationToken cancellationToken) =>
        WriteHealthReportAsync(
            healthChecks,
            registration => registration.Tags.Contains(HealthCheckTags.Ready),
            cancellationToken);

    private static async Task<IResult> WriteHealthReportAsync(
        HealthCheckService healthChecks,
        Func<HealthCheckRegistration, bool> predicate,
        CancellationToken cancellationToken)
    {
        var report = await healthChecks
            .CheckHealthAsync(predicate, cancellationToken)
            .ConfigureAwait(false);

        var response = HealthReportResponse.FromReport(report);
        var statusCode = report.Status is HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return Results.Json(response, statusCode: statusCode);
    }
}

public sealed record HealthReportResponse(
    string Status,
    IReadOnlyDictionary<string, HealthComponentResponse> Components)
{
    public static HealthReportResponse FromReport(HealthReport report)
    {
        var components = report.Entries.ToDictionary(
            static entry => entry.Key,
            static entry => new HealthComponentResponse(
                entry.Value.Status.ToString(),
                entry.Value.Description));

        return new HealthReportResponse(report.Status.ToString(), components);
    }
}

public sealed record HealthComponentResponse(string Status, string? Description);
