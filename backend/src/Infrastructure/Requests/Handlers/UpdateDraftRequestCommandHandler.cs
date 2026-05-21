using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class UpdateDraftRequestCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
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

        RequestMutationHelper.ApplyMutation(request, command.Body, department);
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedBy = currentUser.UserId;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
