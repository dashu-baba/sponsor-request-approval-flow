using MediatR;
using SponsorshipApproval.Application.Audit.Models;
using SponsorshipApproval.Application.Audit.Queries;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;

namespace SponsorshipApproval.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var audit = app.MapGroup("/audit")
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithTags("Audit");

        audit.MapGet("/", ListAsync)
            .WithSummary("List admin audit events")
            .WithDescription(
                "Returns a paginated, filterable audit trail for SystemAdmin. Isolated from workflow history.")
            .Produces<PagedResult<AuditEventDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> ListAsync(
        int? page,
        int? pageSize,
        string? action,
        string? category,
        string? actorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? resourceType,
        string? resourceId,
        string? requestId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListAuditEventsQuery(
                page ?? 1,
                pageSize ?? RequestValidationConstants.DefaultPageSize,
                action,
                category,
                actorId,
                from,
                to,
                resourceType,
                resourceId,
                requestId),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(result);
    }
}
