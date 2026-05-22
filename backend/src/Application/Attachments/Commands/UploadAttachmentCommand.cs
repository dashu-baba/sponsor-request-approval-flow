using MediatR;
using SponsorshipApproval.Application.Attachments.Models;

namespace SponsorshipApproval.Application.Attachments.Commands;

public sealed record UploadAttachmentCommand(
    long RequestId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest<AttachmentDto>;
