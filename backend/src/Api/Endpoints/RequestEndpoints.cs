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

        readGroup.MapGet("/summary", GetSummaryAsync);
        readGroup.MapGet("/", ListAsync);
        readGroup.MapGet("/{id:long}", GetByIdAsync);
        readGroup.MapGet("/{id:long}/history", GetHistoryAsync);
        readGroup.MapAttachmentEndpoints();

        // Write routes: Requestor only
        var requestorGroup = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization(AuthorizationPolicies.Requestor);

        requestorGroup.MapPost("/", CreateAsync);
        requestorGroup.MapPut("/{id:long}", UpdateDraftAsync);

        // Workflow transition routes
        var workflowGroup = app.MapGroup("/requests")
            .WithTags("Requests");

        workflowGroup.MapPost("/{id:long}/submit", SubmitAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:long}/cancel", CancelAsync).RequireAuthorization(AuthorizationPolicies.Requestor);
        workflowGroup.MapPost("/{id:long}/approve", ApproveAsync).RequireAuthorization(AuthorizationPolicies.Approver);
        workflowGroup.MapPost("/{id:long}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.Approver);

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

    private static async Task<IResult> GetSummaryAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequestSummaryQuery(), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        long id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRequestByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetHistoryAsync(
        long id,
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
        long id,
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
        long id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitRequestCommand(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CancelAsync(
        long id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ApproveAsync(
        long id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RejectAsync(
        long id,
        TransitionBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectRequestCommand(id, body.Remarks), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }
}
