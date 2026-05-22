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

        sponsorshipTypes.MapGet("/", ListAsync).RequireAuthorization();
        sponsorshipTypes.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.SystemAdmin);
        sponsorshipTypes.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.SystemAdmin);
        sponsorshipTypes.MapDelete("/{id:guid}", DeleteAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithSummary("Soft-delete a sponsorship type")
            .WithDescription(
                "Sets IsActive=false. Referenced requests are preserved; new requests cannot select this type.");

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
        Guid id,
        SponsorshipTypeMutationBody body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateSponsorshipTypeCommand(id, body), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteSponsorshipTypeCommand(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
