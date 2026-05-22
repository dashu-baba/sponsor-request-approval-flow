using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class DeleteSponsorshipTypeCommandHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IAuditService auditService)
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

        auditService.Record(new AuditRecord(
            currentUser.UserId,
            AuditActions.SponsorshipTypeDeactivated,
            AuditCategories.SponsorshipType,
            AuditResourceTypes.SponsorshipType,
            sponsorshipType.Id.ToString(),
            Summary: $"Deactivated sponsorship type {sponsorshipType.Name}",
            Metadata: new Dictionary<string, object?>
            {
                ["name"] = sponsorshipType.Name,
                ["wasActive"] = true,
            }));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
