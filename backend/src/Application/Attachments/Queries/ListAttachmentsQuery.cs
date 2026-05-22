using MediatR;
using SponsorshipApproval.Application.Attachments.Models;

namespace SponsorshipApproval.Application.Attachments.Queries;

public sealed record ListAttachmentsQuery(long RequestId) : IRequest<IReadOnlyList<AttachmentDto>>;
