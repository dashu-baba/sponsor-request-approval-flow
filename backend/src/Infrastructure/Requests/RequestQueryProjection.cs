using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Infrastructure.Requests;

internal static class RequestQueryProjection
{
    public static IQueryable<RequestDetailDto> SelectDetailDto(this IQueryable<SponsorshipRequest> requests) =>
        requests.Select(request => new RequestDetailDto(
            request.Id,
            request.Title,
            request.RequestorName,
            request.RequestorId,
            request.Department,
            request.SponsorshipTypeId,
            request.SponsorshipType!.Name,
            request.EventName,
            request.EventDate,
            request.RequestedAmount,
            request.Purpose,
            request.ExpectedBenefit,
            request.Remarks,
            request.Status,
            request.CreatedAt,
            request.UpdatedAt));
}
