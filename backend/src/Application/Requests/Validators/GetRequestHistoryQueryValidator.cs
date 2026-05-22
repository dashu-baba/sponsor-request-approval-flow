using FluentValidation;
using SponsorshipApproval.Application.Requests.Queries;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class GetRequestHistoryQueryValidator : AbstractValidator<GetRequestHistoryQuery>
{
    public GetRequestHistoryQueryValidator()
    {
        RuleFor(query => query.RequestId).GreaterThan(0);
    }
}
