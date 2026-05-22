namespace SponsorshipApproval.Application.Requests.Models;

public sealed record RequestSummaryDto(
    int Total,
    int Draft,
    int PendingManagerApproval,
    int PendingFinanceReview,
    int Approved,
    int Rejected,
    int Cancelled);
