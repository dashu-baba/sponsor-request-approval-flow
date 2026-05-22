using MediatR;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Api.Endpoints;

public static class RequestEndpoints
{
    public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder app)
    {
        // Read-only routes: accessible to all authenticated roles; handler enforces visibility/scoping
        var readGroup = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization();

        readGroup.MapGet("/", ListAsync);
        readGroup.MapGet("/{id:guid}", GetByIdAsync);
        readGroup.MapGet("/{id:guid}/history", GetHistoryAsync);
        readGroup.MapAttachmentEndpoints();

        // Write routes: Requestor only
        var requestorGroup = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization(AuthorizationPolicies.Requestor);

        requestorGroup.MapPost("/", CreateAsync);
        requestorGroup.MapPut("/{id:guid}", UpdateDraftAsync);

        // Workflow transition routes
        var workflowGroup = app.MapGroup("/requests")
            .WithTags("Requests");

        workflowGroup.MapPost("/{id:guid}/submit", SubmitAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:guid}/cancel", CancelAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(AuthorizationPolicies.Approver);
        workflowGroup.MapPost("/{id:guid}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.Approver);

        return app;
    }

    private static async Task<IResult> ListAsync(
        int? page,
        int? pageSize,
        RequestStatus? status,
        IMediator mediator,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var p = page ?? 1;
        var ps = pageSize ?? RequestValidationConstants.DefaultPageSize;

        if (currentUser.Roles.Contains(Roles.Manager))
        {
            var result = await mediator.Send(new ListManagerQueueQuery(p, ps), cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(result);
        }

        if (currentUser.Roles.Contains(Roles.FinanceAdmin))
        {
            var result = await mediator.Send(new ListFinanceQueueQuery(p, ps), cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(result);
        }

        if (currentUser.Roles.Contains(Roles.SystemAdmin))
        {
            var result = await mediator.Send(new ListAdminRequestsQuery(p, ps, status), cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(result);
        }

        // Default: Requestor sees own requests
        var ownResult = await mediator.Send(
            new ListOwnRequestsQuery(p, ps),
            cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(ownResult);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequestByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequestHistoryQuery(id), cancellationToken).ConfigureAwait(false);
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
