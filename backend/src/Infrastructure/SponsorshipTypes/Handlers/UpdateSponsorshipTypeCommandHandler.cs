using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class UpdateSponsorshipTypeCommandHandler(
    AppDbContext dbContext,
    ICurrentUserContext currentUser,
    IAuditService auditService)
    : IRequestHandler<UpdateSponsorshipTypeCommand, SponsorshipTypeDto>
{
    public async Task<SponsorshipTypeDto> Handle(
        UpdateSponsorshipTypeCommand command,
        CancellationToken cancellationToken)
    {
        var sponsorshipType = await dbContext.SponsorshipTypes
            .SingleOrDefaultAsync(type => type.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (sponsorshipType is null)
        {
            throw new NotFoundException("Sponsorship type was not found.");
        }

        var name = command.Body.Name.Trim();
        await CreateSponsorshipTypeCommandHandler
            .EnsureActiveNameIsUniqueAsync(dbContext, name, command.Id, cancellationToken)
            .ConfigureAwait(false);

        sponsorshipType.Name = name;
        sponsorshipType.Description = CreateSponsorshipTypeCommandHandler.NormalizeDescription(command.Body.Description);
        sponsorshipType.UpdatedAt = DateTimeOffset.UtcNow;
        sponsorshipType.UpdatedBy = currentUser.UserId;

        auditService.Record(new AuditRecord(
            currentUser.UserId,
            AuditActions.SponsorshipTypeUpdated,
            AuditCategories.SponsorshipType,
            AuditResourceTypes.SponsorshipType,
            sponsorshipType.Id.ToString(),
            Summary: $"Updated sponsorship type {name}",
            Metadata: new Dictionary<string, object?> { ["name"] = name }));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SponsorshipTypes
            .AsNoTracking()
            .Where(type => type.Id == sponsorshipType.Id)
            .Select(SponsorshipTypeProjection.ToDto)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
