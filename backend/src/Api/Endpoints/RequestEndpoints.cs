using MediatR;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;

namespace SponsorshipApproval.Api.Endpoints;

public static class RequestEndpoints
{
    public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var requestorGroup = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization(AuthorizationPolicies.Requestor);

        requestorGroup.MapGet("/", ListOwnAsync);
        requestorGroup.MapPost("/", CreateAsync);
        requestorGroup.MapGet("/{id:guid}", GetByIdAsync);
        requestorGroup.MapPut("/{id:guid}", UpdateDraftAsync);
        requestorGroup.MapAttachmentEndpoints();

        // Workflow transition endpoints: any authenticated user can reach the route;
        // role and ownership enforcement is handled by the state machine and handler.
        var workflowGroup = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization();

        workflowGroup.MapPost("/{id:guid}/submit", SubmitAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:guid}/cancel", CancelAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(AuthorizationPolicies.Approver);
        workflowGroup.MapPost("/{id:guid}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.Approver);

        return app;
    }

    private static async Task<IResult> ListOwnAsync(
        int? page,
        int? pageSize,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListOwnRequestsQuery(
                page ?? 1,
                pageSize ?? RequestValidationConstants.DefaultPageSize),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateAsync(
        RequestMutationBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRequestCommand(body), cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/requests/{result.Id}", result);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequestByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid id,
        RequestMutationBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator
            .Send(new UpdateDraftRequestCommand(id, body), cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitRequestCommand(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }
}
