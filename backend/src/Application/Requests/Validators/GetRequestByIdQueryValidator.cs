using FluentValidation;
using SponsorshipApproval.Application.Requests.Queries;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class GetRequestByIdQueryValidator : AbstractValidator<GetRequestByIdQuery>
{
    public GetRequestByIdQueryValidator()
    {
        RuleFor(query => query.Id).GreaterThan(0);
    }
}
