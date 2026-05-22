using FluentValidation;
using SponsorshipApproval.Application.Audit.Queries;
using SponsorshipApproval.Application.Common;

namespace SponsorshipApproval.Application.Audit.Validators;

public sealed class ListAuditEventsQueryValidator : AbstractValidator<ListAuditEventsQuery>
{
    public ListAuditEventsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, RequestValidationConstants.MaxPageSize);
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("'To' must be on or after 'From'.");
    }
}
