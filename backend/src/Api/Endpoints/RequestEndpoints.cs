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
        var requests = app.MapGroup("/requests")
            .WithTags("Requests")
            .RequireAuthorization(AuthorizationPolicies.Requestor);

        requests.MapGet("/", ListOwnAsync);
        requests.MapPost("/", CreateAsync);
        requests.MapGet("/{id:guid}", GetByIdAsync);
        requests.MapPut("/{id:guid}", UpdateDraftAsync);

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
}
