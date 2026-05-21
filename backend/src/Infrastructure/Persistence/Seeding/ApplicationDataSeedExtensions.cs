using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Persistence.Seeding;

public static class ApplicationDataSeedExtensions
{
    public static async Task SeedApplicationDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await dbContext.SponsorshipRequests
                .AnyAsync(request => request.Id == SeedData.Requests.Draft)
                .ConfigureAwait(false))
        {
            return;
        }

        dbContext.SponsorshipTypes.AddRange(CreateSponsorshipTypes());
        dbContext.SponsorshipRequests.AddRange(CreateSponsorshipRequests());
        dbContext.WorkflowHistoryEntries.AddRange(CreateWorkflowHistory());

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static IEnumerable<SponsorshipType> CreateSponsorshipTypes()
    {
        yield return CreateSponsorshipType(
            SeedData.SponsorshipTypes.Conference,
            "Conference",
            "Industry conferences, summits, and professional events.");

        yield return CreateSponsorshipType(
            SeedData.SponsorshipTypes.CommunityEvent,
            "Community Event",
            "Local community outreach and public engagement activities.");

        yield return CreateSponsorshipType(
            SeedData.SponsorshipTypes.Sports,
            "Sports Sponsorship",
            "Athletic events, teams, and wellness initiatives.");

        yield return CreateSponsorshipType(
            SeedData.SponsorshipTypes.Educational,
            "Educational Program",
            "Training, workshops, and educational partnerships.");
    }

    private static SponsorshipType CreateSponsorshipType(Guid id, string name, string description)
    {
        return new SponsorshipType
        {
            Id = id,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = SeedData.BaseTimestamp,
            CreatedBy = SeedData.Users.SystemAdmin.Id,
        };
    }

    private static IEnumerable<SponsorshipRequest> CreateSponsorshipRequests()
    {
        yield return CreateRequest(
            SeedData.Requests.Draft,
            "Annual Tech Conference Booth",
            RequestStatus.Draft,
            SeedData.SponsorshipTypes.Conference,
            "TechForward Summit",
            new DateOnly(2026, 9, 15),
            3500m,
            "Secure a booth to showcase our engineering initiatives.",
            remarks: "Draft — not yet submitted.");

        yield return CreateRequest(
            SeedData.Requests.PendingManagerApproval,
            "Regional Developer Meetup Sponsorship",
            RequestStatus.PendingManagerApproval,
            SeedData.SponsorshipTypes.CommunityEvent,
            "Regional Dev Meetup",
            new DateOnly(2026, 8, 20),
            1200m,
            "Support a local developer community event.",
            remarks: "Awaiting manager review.");

        yield return CreateRequest(
            SeedData.Requests.PendingFinanceReview,
            "Charity Fun Run Partnership",
            RequestStatus.PendingFinanceReview,
            SeedData.SponsorshipTypes.Sports,
            "City Charity Fun Run",
            new DateOnly(2026, 10, 5),
            2500m,
            "Sponsor a charity run to support community wellness.",
            remarks: "Manager approved; awaiting finance review.");

        yield return CreateRequest(
            SeedData.Requests.Approved,
            "STEM Scholarship Workshop Series",
            RequestStatus.Approved,
            SeedData.SponsorshipTypes.Educational,
            "Future Innovators Workshop",
            new DateOnly(2026, 11, 12),
            5000m,
            "Fund hands-on STEM workshops for local students.",
            remarks: "Fully approved.");

        yield return CreateRequest(
            SeedData.Requests.Rejected,
            "Premium Hospitality Package",
            RequestStatus.Rejected,
            SeedData.SponsorshipTypes.Conference,
            "Executive Leadership Forum",
            new DateOnly(2026, 7, 30),
            15000m,
            "Premium sponsorship tier for an executive forum.",
            remarks: "Rejected by manager.");

        yield return CreateRequest(
            SeedData.Requests.Cancelled,
            "Startup Pitch Night Sponsorship",
            RequestStatus.Cancelled,
            SeedData.SponsorshipTypes.CommunityEvent,
            "Startup Pitch Night",
            new DateOnly(2026, 6, 18),
            800m,
            "Support a startup pitch event for local entrepreneurs.",
            remarks: "Cancelled by requestor.");
    }

    private static SponsorshipRequest CreateRequest(
        Guid id,
        string title,
        RequestStatus status,
        Guid sponsorshipTypeId,
        string eventName,
        DateOnly eventDate,
        decimal requestedAmount,
        string purpose,
        string remarks)
    {
        return new SponsorshipRequest
        {
            Id = id,
            Title = title,
            RequestorName = SeedData.Users.Requestor.DisplayName,
            RequestorId = SeedData.Users.Requestor.Id,
            Department = SeedData.Users.Requestor.Department,
            SponsorshipTypeId = sponsorshipTypeId,
            EventName = eventName,
            EventDate = eventDate,
            RequestedAmount = requestedAmount,
            Purpose = purpose,
            ExpectedBenefit = "Increase brand visibility and community engagement.",
            Remarks = remarks,
            Status = status,
            CreatedAt = SeedData.BaseTimestamp,
            CreatedBy = SeedData.Users.Requestor.Id,
            UpdatedAt = status == RequestStatus.Draft ? null : SeedData.BaseTimestamp.AddHours(1),
            UpdatedBy = status == RequestStatus.Draft ? null : SeedData.Users.Requestor.Id,
        };
    }

    private static IEnumerable<WorkflowHistory> CreateWorkflowHistory()
    {
        foreach (var transition in SeedData.WorkflowTrails.PendingManagerApproval)
        {
            yield return CreateHistoryEntry(SeedData.Requests.PendingManagerApproval, transition);
        }

        foreach (var transition in SeedData.WorkflowTrails.PendingFinanceReview)
        {
            yield return CreateHistoryEntry(SeedData.Requests.PendingFinanceReview, transition);
        }

        foreach (var transition in SeedData.WorkflowTrails.Approved)
        {
            yield return CreateHistoryEntry(SeedData.Requests.Approved, transition);
        }

        foreach (var transition in SeedData.WorkflowTrails.Rejected)
        {
            yield return CreateHistoryEntry(SeedData.Requests.Rejected, transition);
        }

        foreach (var transition in SeedData.WorkflowTrails.Cancelled)
        {
            yield return CreateHistoryEntry(SeedData.Requests.Cancelled, transition);
        }
    }

    private static WorkflowHistory CreateHistoryEntry(Guid requestId, SeedData.SeedTransition transition)
    {
        return new WorkflowHistory
        {
            Id = transition.HistoryId,
            SponsorshipRequestId = requestId,
            ActorId = transition.ActorId,
            FromStatus = transition.FromStatus,
            ToStatus = transition.ToStatus,
            Remarks = transition.Remarks,
            OccurredAt = SeedData.BaseTimestamp.Add(transition.Offset),
        };
    }
}
