using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Attachments.Queries;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Common.Storage;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.Requests;

namespace SponsorshipApproval.Infrastructure.Attachments.Handlers;

public sealed class DownloadAttachmentQueryHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IObjectStorage objectStorage)
    : IRequestHandler<DownloadAttachmentQuery, AttachmentDownloadResult>
{
    public async Task<AttachmentDownloadResult> Handle(
        DownloadAttachmentQuery query,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == query.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        RequestMutationHelper.EnsureOwner(request, currentUser.UserId);

        var attachment = await dbContext.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == query.AttachmentId && entity.SponsorshipRequestId == query.RequestId,
                cancellationToken)
            .ConfigureAwait(false);

        if (attachment is null)
        {
            throw new NotFoundException("Attachment was not found.");
        }

        var storedObject = await objectStorage
            .GetAsync(attachment.ObjectKey, cancellationToken)
            .ConfigureAwait(false);

        return new AttachmentDownloadResult(
            storedObject,
            attachment.ContentType,
            attachment.FileName);
    }
}
