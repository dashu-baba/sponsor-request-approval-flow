using SponsorshipApproval.Application.Auth;

namespace SponsorshipApproval.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/system/ping", () => Results.NoContent())
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithTags("System")
            .WithSummary("System admin ping")
            .WithDescription("No-op endpoint reserved for system administration checks.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
