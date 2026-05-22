using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class CreateRequestCommandHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IAuditService auditService)
    : IRequestHandler<CreateRequestCommand, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var profileDepartment = await currentUser.GetDepartmentAsync(cancellationToken).ConfigureAwait(false);
        var department = await RequestMutationHelper
            .ResolveDepartmentAsync(command.Body, profileDepartment, cancellationToken)
            .ConfigureAwait(false);

        await RequestMutationHelper
            .GetActiveSponsorshipTypeAsync(dbContext, command.Body.SponsorshipTypeId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var request = new SponsorshipRequest
        {
            RequestorId = currentUser.UserId,
            RequestorName = currentUser.DisplayName,
            Status = RequestStatus.Draft,
            CreatedAt = now,
            CreatedBy = currentUser.UserId,
        };

        RequestMutationHelper.ApplyMutation(request, command.Body, department);
        dbContext.SponsorshipRequests.Add(request);

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            auditService.Record(new AuditRecord(
                currentUser.UserId,
                AuditActions.RequestCreated,
                AuditCategories.Request,
                AuditResourceTypes.SponsorshipRequest,
                request.Id.ToString(),
                Summary: "Created draft request",
                Metadata: new Dictionary<string, object?> { ["requestId"] = request.Id.ToString() }));

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

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
