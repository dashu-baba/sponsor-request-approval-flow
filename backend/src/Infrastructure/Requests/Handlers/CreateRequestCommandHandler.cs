using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Commands;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests.Handlers;

public sealed class CreateRequestCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
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
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SponsorshipRequests
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .SelectDetailDto()
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
