using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Attachments.Models;
using SponsorshipApproval.Application.Attachments.Queries;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Attachments.Handlers;

public sealed class ListAttachmentsQueryHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<ListAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<IReadOnlyList<AttachmentDto>> Handle(
        ListAttachmentsQuery query,
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

        var isOwner = string.Equals(request.RequestorId, currentUser.UserId, StringComparison.Ordinal);
        var isReviewer = currentUser.Roles.Contains(Roles.Manager)
            || currentUser.Roles.Contains(Roles.FinanceAdmin)
            || currentUser.Roles.Contains(Roles.SystemAdmin);

        if (!isOwner && !isReviewer)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        return await dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.SponsorshipRequestId == query.RequestId)
            .OrderBy(attachment => attachment.CreatedAt)
            .Select(attachment => new AttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
