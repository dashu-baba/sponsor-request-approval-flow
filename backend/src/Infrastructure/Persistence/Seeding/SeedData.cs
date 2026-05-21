using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Persistence.Seeding;

internal static class SeedData
{
    public const string DefaultPassword = "Password1!";

    public static readonly DateTimeOffset BaseTimestamp = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    internal sealed record SeedUser(string Id, string Email, string DisplayName, string Department, string Role);

    internal static class Users
    {
        public static readonly SeedUser Requestor = new(
            "seed-requestor",
            "requestor@demo.local",
            "Alex Requestor",
            "Engineering",
            Roles.Requestor);

        public static readonly SeedUser Manager = new(
            "seed-manager",
            "manager@demo.local",
            "Morgan Manager",
            "Operations",
            Roles.Manager);

        public static readonly SeedUser FinanceAdmin = new(
            "seed-finance",
            "finance@demo.local",
            "Finley Finance",
            "Finance",
            Roles.FinanceAdmin);

        public static readonly SeedUser SystemAdmin = new(
            "seed-admin",
            "admin@demo.local",
            "Sam Admin",
            "IT",
            Roles.SystemAdmin);

        public static IReadOnlyList<SeedUser> All { get; } =
        [
            Requestor,
            Manager,
            FinanceAdmin,
            SystemAdmin,
        ];
    }

    internal static class SponsorshipTypes
    {
        public static readonly Guid Conference = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

        public static readonly Guid CommunityEvent = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

        public static readonly Guid Sports = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");

        public static readonly Guid Educational = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
    }

    internal static class Requests
    {
        public static readonly Guid Draft = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

        public static readonly Guid PendingManagerApproval = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

        public static readonly Guid PendingFinanceReview = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

        public static readonly Guid Approved = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4");

        public static readonly Guid Rejected = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5");

        public static readonly Guid Cancelled = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6");
    }

    internal sealed record SeedTransition(
        Guid HistoryId,
        RequestStatus FromStatus,
        RequestStatus ToStatus,
        string ActorId,
        string? Remarks,
        TimeSpan Offset);

    internal static class WorkflowTrails
    {
        public static IReadOnlyList<SeedTransition> PendingManagerApproval { get; } =
        [
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(10)),
        ];

        public static IReadOnlyList<SeedTransition> PendingFinanceReview { get; } =
        [
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(20)),
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
                RequestStatus.PendingManagerApproval,
                RequestStatus.PendingFinanceReview,
                Users.Manager.Id,
                "Approved by manager.",
                TimeSpan.FromMinutes(30)),
        ];

        public static IReadOnlyList<SeedTransition> Approved { get; } =
        [
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc4"),
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(40)),
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc5"),
                RequestStatus.PendingManagerApproval,
                RequestStatus.PendingFinanceReview,
                Users.Manager.Id,
                "Approved by manager.",
                TimeSpan.FromMinutes(50)),
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc6"),
                RequestStatus.PendingFinanceReview,
                RequestStatus.Approved,
                Users.FinanceAdmin.Id,
                "Approved by finance.",
                TimeSpan.FromMinutes(60)),
        ];

        public static IReadOnlyList<SeedTransition> Rejected { get; } =
        [
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc7"),
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(70)),
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc8"),
                RequestStatus.PendingManagerApproval,
                RequestStatus.Rejected,
                Users.Manager.Id,
                "Budget not aligned with current priorities.",
                TimeSpan.FromMinutes(80)),
        ];

        public static IReadOnlyList<SeedTransition> Cancelled { get; } =
        [
            new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc9"),
                RequestStatus.Draft,
                RequestStatus.Cancelled,
                Users.Requestor.Id,
                "Event postponed by organizer.",
                TimeSpan.FromMinutes(90)),
        ];
    }
}
