using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Infrastructure.Audit;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class UpdateDraftRequestCommandHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IAuditService auditService)
    : IRequestHandler<UpdateDraftRequestCommand, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(UpdateDraftRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.SponsorshipRequests
            .SingleOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            throw new NotFoundException("Request was not found.");
        }

        RequestMutationHelper.EnsureOwner(request, currentUser.UserId);
        RequestMutationHelper.EnsureDraft(request);

        var profileDepartment = await currentUser.GetDepartmentAsync(cancellationToken).ConfigureAwait(false);
        var department = await RequestMutationHelper
            .ResolveDepartmentAsync(command.Body, profileDepartment, cancellationToken)
            .ConfigureAwait(false);

        await RequestMutationHelper
            .GetActiveSponsorshipTypeAsync(dbContext, command.Body.SponsorshipTypeId, cancellationToken)
            .ConfigureAwait(false);

        var before = SnapshotRequest(request);
        RequestMutationHelper.ApplyMutation(request, command.Body, department);
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = currentUser.UserId;

        var changedFields = DraftChangeTracker.GetChangedFieldNames(before, request);
        if (changedFields.Count > 0)
        {
            auditService.Record(new AuditRecord(
                currentUser.UserId,
                AuditActions.RequestUpdated,
                AuditCategories.Request,
                AuditResourceTypes.SponsorshipRequest,
                request.Id.ToString(),
                Summary: $"Updated draft fields: {string.Join(", ", changedFields)}",
                Metadata: new Dictionary<string, object?>
                {
                    ["requestId"] = request.Id.ToString(),
                    ["changedFields"] = changedFields.ToArray(),
                }));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static Domain.Requests.SponsorshipRequest SnapshotRequest(Domain.Requests.SponsorshipRequest request) =>
        new()
        {
            Title = request.Title,
            Department = request.Department,
            SponsorshipTypeId = request.SponsorshipTypeId,
            EventName = request.EventName,
            EventDate = request.EventDate,
            RequestedAmount = request.RequestedAmount,
            Purpose = request.Purpose,
            ExpectedBenefit = request.ExpectedBenefit,
            Remarks = request.Remarks,
        };
}
