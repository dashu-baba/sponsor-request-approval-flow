using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Attachments;
using SponsorshipApproval.Application.Attachments.Commands;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Audit;
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
    IObjectStorage objectStorage,
    IAuditService auditService)
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
            var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                auditService.Record(new AuditRecord(
                    currentUser.UserId,
                    AuditActions.AttachmentUploaded,
                    AuditCategories.Attachment,
                    AuditResourceTypes.Attachment,
                    attachment.Id.ToString(),
                    Summary: $"Uploaded attachment {sanitizedFileName}",
                    Metadata: new Dictionary<string, object?>
                    {
                        ["requestId"] = command.RequestId.ToString(),
                        ["fileName"] = sanitizedFileName,
                        ["contentType"] = command.ContentType,
                        ["sizeBytes"] = command.SizeBytes,
                    }));

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
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
