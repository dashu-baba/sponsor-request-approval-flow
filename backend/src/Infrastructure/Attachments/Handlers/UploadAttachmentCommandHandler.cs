using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Attachments;
using SponsorshipApproval.Application.Attachments.Commands;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Common.Storage;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.Requests;

namespace SponsorshipApproval.Infrastructure.Attachments.Handlers;

public sealed class UploadAttachmentCommandHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IObjectStorage objectStorage)
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .SingleOrDefaultAsync(entity => entity.Id == command.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        RequestMutationHelper.EnsureOwner(request, currentUser.UserId);
        RequestMutationHelper.EnsureDraft(request);

        var extension = AttachmentFileValidator.ValidateAndResolveExtension(
            command.FileName,
            command.ContentType,
            command.SizeBytes);

        await AttachmentFileValidator
            .ValidateContentSignatureAsync(command.Content, command.ContentType, cancellationToken)
            .ConfigureAwait(false);

        var objectKey = AttachmentObjectKeyGenerator.Generate(command.RequestId, extension);
        var sanitizedFileName = Path.GetFileName(command.FileName);

        await objectStorage
            .UploadAsync(objectKey, command.Content, command.ContentType, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            SponsorshipRequestId = command.RequestId,
            ObjectKey = objectKey,
            FileName = sanitizedFileName,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            CreatedAt = now,
            CreatedBy = currentUser.UserId,
        };

        dbContext.Attachments.Add(attachment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await objectStorage.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
            throw;
        }

        return new AttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt);
    }
}
