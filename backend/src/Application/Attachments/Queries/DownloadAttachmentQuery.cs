using MediatR;
using SponsorshipApproval.Application.Attachments.Models;

namespace SponsorshipApproval.Application.Attachments.Queries;

public sealed record DownloadAttachmentQuery(Guid RequestId, Guid AttachmentId)
    : IRequest<AttachmentDownloadResult>;
