using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Audit;

internal static class DraftChangeTracker
{
    public static IReadOnlyList<string> GetChangedFieldNames(SponsorshipRequest before, SponsorshipRequest after)
    {
        var changed = new List<string>();

        if (!string.Equals(before.Title, after.Title, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.Title));
        }

        if (!string.Equals(before.Department, after.Department, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.Department));
        }

        if (before.SponsorshipTypeId != after.SponsorshipTypeId)
        {
            changed.Add(nameof(SponsorshipRequest.SponsorshipTypeId));
        }

        if (!string.Equals(before.EventName, after.EventName, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.EventName));
        }

        if (before.EventDate != after.EventDate)
        {
            changed.Add(nameof(SponsorshipRequest.EventDate));
        }

        if (before.RequestedAmount != after.RequestedAmount)
        {
            changed.Add(nameof(SponsorshipRequest.RequestedAmount));
        }

        if (!string.Equals(before.Purpose, after.Purpose, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.Purpose));
        }

        if (!string.Equals(before.ExpectedBenefit, after.ExpectedBenefit, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.ExpectedBenefit));
        }

        if (!string.Equals(before.Remarks, after.Remarks, StringComparison.Ordinal))
        {
            changed.Add(nameof(SponsorshipRequest.Remarks));
        }

        return changed;
    }
}
