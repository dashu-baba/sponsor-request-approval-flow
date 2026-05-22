using MediatR;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Application.SponsorshipTypes.Queries;

namespace SponsorshipApproval.Api.Endpoints;

public static class SponsorshipTypeEndpoints
{
    public static IEndpointRouteBuilder MapSponsorshipTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var sponsorshipTypes = app.MapGroup("/sponsorship-types")
            .WithTags("Sponsorship Types");

        sponsorshipTypes.MapGet("/", ListAsync)
            .RequireAuthorization()
            .WithSummary("List sponsorship types")
            .WithDescription("Returns active types for non-admin roles; SystemAdmin sees inactive types as well.")
            .Produces<IReadOnlyList<SponsorshipTypeDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        sponsorshipTypes.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithSummary("Create a sponsorship type")
            .Produces<SponsorshipTypeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        sponsorshipTypes.MapPut("/{id:long}", UpdateAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithSummary("Update a sponsorship type")
            .Produces<SponsorshipTypeDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        sponsorshipTypes.MapDelete("/{id:long}", DeleteAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithSummary("Soft-delete a sponsorship type")
            .WithDescription(
                "Sets IsActive=false. Referenced requests are preserved; new requests cannot select this type.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListSponsorshipTypesQuery(), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateAsync(
        SponsorshipTypeMutationBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateSponsorshipTypeCommand(body), cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/sponsorship-types/{result.Id}", result);
    }

    private static async Task<IResult> UpdateAsync(
        long id,
        SponsorshipTypeMutationBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateSponsorshipTypeCommand(id, body), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        long id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteSponsorshipTypeCommand(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
