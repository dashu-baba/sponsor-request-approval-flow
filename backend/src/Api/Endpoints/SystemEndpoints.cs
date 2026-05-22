namespace SponsorshipApproval.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/system/ping", () => Results.NoContent())
            .AllowAnonymous()
            .WithTags("System")
            .WithSummary("System ping")
            .WithDescription("Public no-op endpoint for connectivity checks.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
