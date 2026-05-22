using MediatR;
using SponsorshipApproval.Application.Attachments.Models;

namespace SponsorshipApproval.Application.Attachments.Queries;

public sealed record DownloadAttachmentQuery(long RequestId, long AttachmentId)
    : IRequest<AttachmentDownloadResult>;
