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
        public const long Conference = 1;

        public const long CommunityEvent = 2;

        public const long Sports = 3;

        public const long Educational = 4;
    }

    internal static class Requests
    {
        public const long Draft = 1;

        public const long PendingManagerApproval = 2;

        public const long PendingFinanceReview = 3;

        public const long Approved = 4;

        public const long Rejected = 5;

        public const long Cancelled = 6;
    }

    internal sealed record SeedTransition(
        long HistoryId,
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
                1,
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(10)),
        ];

        public static IReadOnlyList<SeedTransition> PendingFinanceReview { get; } =
        [
            new(
                2,
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(20)),
            new(
                3,
                RequestStatus.PendingManagerApproval,
                RequestStatus.PendingFinanceReview,
                Users.Manager.Id,
                "Approved by manager.",
                TimeSpan.FromMinutes(30)),
        ];

        public static IReadOnlyList<SeedTransition> Approved { get; } =
        [
            new(
                4,
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(40)),
            new(
                5,
                RequestStatus.PendingManagerApproval,
                RequestStatus.PendingFinanceReview,
                Users.Manager.Id,
                "Approved by manager.",
                TimeSpan.FromMinutes(50)),
            new(
                6,
                RequestStatus.PendingFinanceReview,
                RequestStatus.Approved,
                Users.FinanceAdmin.Id,
                "Approved by finance.",
                TimeSpan.FromMinutes(60)),
        ];

        public static IReadOnlyList<SeedTransition> Rejected { get; } =
        [
            new(
                7,
                RequestStatus.Draft,
                RequestStatus.PendingManagerApproval,
                Users.Requestor.Id,
                "Submitted for manager approval.",
                TimeSpan.FromMinutes(70)),
            new(
                8,
                RequestStatus.PendingManagerApproval,
                RequestStatus.Rejected,
                Users.Manager.Id,
                "Budget not aligned with current priorities.",
                TimeSpan.FromMinutes(80)),
        ];

        public static IReadOnlyList<SeedTransition> Cancelled { get; } =
        [
            new(
                9,
                RequestStatus.Draft,
                RequestStatus.Cancelled,
                Users.Requestor.Id,
                "Event postponed by organizer.",
                TimeSpan.FromMinutes(90)),
        ];
    }
}
