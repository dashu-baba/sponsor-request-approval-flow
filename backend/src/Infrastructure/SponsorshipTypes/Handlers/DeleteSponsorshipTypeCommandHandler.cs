using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class DeleteSponsorshipTypeCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<DeleteSponsorshipTypeCommand>
{
    public async Task Handle(DeleteSponsorshipTypeCommand command, CancellationToken cancellationToken)
    {
        var sponsorshipType = await dbContext.SponsorshipTypes
            .SingleOrDefaultAsync(type => type.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (sponsorshipType is null)
        {
            throw new NotFoundException("Sponsorship type was not found.");
        }

        if (!sponsorshipType.IsActive)
        {
            return;
        }

        sponsorshipType.IsActive = false;
        sponsorshipType.UpdatedAt = DateTimeOffset.UtcNow;
        sponsorshipType.UpdatedBy = currentUser.UserId;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
