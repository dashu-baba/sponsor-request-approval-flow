using SponsorshipApproval.Application.Auth;

namespace SponsorshipApproval.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/system/ping", () => Results.NoContent())
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithTags("System");

        return app;
    }
}
