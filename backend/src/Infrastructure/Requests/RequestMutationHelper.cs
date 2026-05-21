using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Common.Exceptions;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Requests;

internal static class RequestMutationHelper
{
    public static Task<string> ResolveDepartmentAsync(
        RequestMutationBody body,
        string? profileDepartment,
        CancellationToken cancellationToken)
    {
        var department = string.IsNullOrWhiteSpace(body.Department)
            ? profileDepartment
            : body.Department.Trim();

        if (string.IsNullOrWhiteSpace(department))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(RequestMutationBody.Department), "Department is required."),
            ]);
        }

        return Task.FromResult(department);
    }

    public static async Task<SponsorshipType> GetActiveSponsorshipTypeAsync(
        AppDbContext dbContext,
        Guid sponsorshipTypeId,
        CancellationToken cancellationToken)
    {
        var sponsorshipType = await dbContext.SponsorshipTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                type => type.Id == sponsorshipTypeId && type.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (sponsorshipType is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(RequestMutationBody.SponsorshipTypeId),
                    "Sponsorship type is invalid or inactive."),
            ]);
        }

        return sponsorshipType;
    }

    public static void ApplyMutation(
        SponsorshipRequest request,
        RequestMutationBody body,
        string department)
    {
        request.Title = body.Title.Trim();
        request.Department = department;
        request.SponsorshipTypeId = body.SponsorshipTypeId;
        request.EventName = body.EventName.Trim();
        request.EventDate = body.EventDate;
        request.RequestedAmount = body.RequestedAmount;
        request.Purpose = body.Purpose.Trim();
        request.ExpectedBenefit = string.IsNullOrWhiteSpace(body.ExpectedBenefit)
            ? null
            : body.ExpectedBenefit.Trim();
        request.Remarks = string.IsNullOrWhiteSpace(body.Remarks)
            ? null
            : body.Remarks.Trim();
    }

    public static void EnsureOwner(SponsorshipRequest request, string userId)
    {
        if (!string.Equals(request.RequestorId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You do not have access to this request.");
        }
    }

    public static void EnsureDraft(SponsorshipRequest request)
    {
        if (request.Status != RequestStatus.Draft)
        {
            throw new ConflictException("Only draft requests can be edited.");
        }
    }
}
