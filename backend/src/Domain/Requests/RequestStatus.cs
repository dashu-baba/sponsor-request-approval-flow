using System.Text.Json.Serialization;

namespace SponsorshipApproval.Domain.Requests;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    Draft = 0,
    PendingManagerApproval = 1,
    PendingFinanceReview = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
}
