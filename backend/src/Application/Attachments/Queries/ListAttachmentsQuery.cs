using MediatR;
using SponsorshipApproval.Application.Attachments.Models;

namespace SponsorshipApproval.Application.Attachments.Queries;

public sealed record ListAttachmentsQuery(Guid RequestId) : IRequest<IReadOnlyList<AttachmentDto>>;
