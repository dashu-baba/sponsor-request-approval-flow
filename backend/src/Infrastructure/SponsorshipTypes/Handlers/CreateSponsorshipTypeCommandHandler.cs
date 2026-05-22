using MediatR;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;
using SponsorshipApproval.Application.SponsorshipTypes.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.SponsorshipTypes.Handlers;

public sealed class CreateSponsorshipTypeCommandHandler(AppDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<CreateSponsorshipTypeCommand, SponsorshipTypeDto>
{
    public async Task<SponsorshipTypeDto> Handle(
        CreateSponsorshipTypeCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Body.Name.Trim();
        await EnsureActiveNameIsUniqueAsync(dbContext, name, excludedId: null, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var sponsorshipType = new SponsorshipType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeDescription(command.Body.Description),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUser.UserId,
        };

        dbContext.SponsorshipTypes.Add(sponsorshipType);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SponsorshipTypes
            .AsNoTracking()
            .Where(type => type.Id == sponsorshipType.Id)
            .Select(SponsorshipTypeProjection.ToDto)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task EnsureActiveNameIsUniqueAsync(
        AppDbContext dbContext,
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToUpperInvariant();
        var exists = await dbContext.SponsorshipTypes
            .AsNoTracking()
            .AnyAsync(
                type => type.IsActive
                    && type.Name.ToUpper() == normalizedName
                    && (excludedId == null || type.Id != excludedId),
                cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new ConflictException("An active sponsorship type with this name already exists.");
        }
    }

    internal static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
