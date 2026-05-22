using FluentValidation;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Queries;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class ListAdminRequestsQueryValidator : AbstractValidator<ListAdminRequestsQuery>
{
    public ListAdminRequestsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, RequestValidationConstants.MaxPageSize);
        RuleFor(query => query.Status)
            .Must(status => status is null || status != RequestStatus.Draft)
            .WithMessage("Admin list cannot be filtered by Draft status.");
    }
}
