using MediatR;
using SponsorshipApproval.Application.Attachments.Commands;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Attachments.Queries;

namespace SponsorshipApproval.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static RouteGroupBuilder MapAttachmentEndpoints(this RouteGroupBuilder requests)
    {
        requests.MapPost("/{id:long}/attachments", UploadAsync)
            .DisableAntiforgery()
            .WithSummary("Upload an attachment to a request")
            .WithDescription("Multipart upload; max size enforced by server configuration.")
            .Produces<AttachmentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        requests.MapGet("/{id:long}/attachments", ListAsync)
            .WithSummary("List attachments for a request")
            .Produces<IReadOnlyList<AttachmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        requests.MapGet("/{id:long}/attachments/{attachmentId:long}", DownloadAsync)
            .WithSummary("Download an attachment")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return requests;
    }

    private static async Task<IResult> UploadAsync(
        long id,
        IFormFile file,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.Problem(
                title: "Validation failed",
                detail: "File is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadAttachmentCommand(
                id,
                file.FileName,
                file.ContentType,
                file.Length,
                stream),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Created($"/requests/{id}/attachments/{result.Id}", result);
    }

    private static async Task<IResult> ListAsync(
        long id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator
            .Send(new ListAttachmentsQuery(id), cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DownloadAsync(
        long id,
        long attachmentId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator
            .Send(new DownloadAttachmentQuery(id, attachmentId), cancellationToken)
            .ConfigureAwait(false);

        httpContext.Response.RegisterForDispose(result);

        return TypedResults.File(
            result.Content,
            contentType: result.ContentType,
            fileDownloadName: result.FileName,
            enableRangeProcessing: true);
    }
}
