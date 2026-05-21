using FluentValidation;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Queries;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class ListOwnRequestsQueryValidator : AbstractValidator<ListOwnRequestsQuery>
{
    public ListOwnRequestsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, RequestValidationConstants.MaxPageSize);
    }
}
