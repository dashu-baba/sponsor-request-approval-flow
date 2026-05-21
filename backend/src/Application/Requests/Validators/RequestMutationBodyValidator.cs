using FluentValidation;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Requests.Models;

namespace SponsorshipApproval.Application.Requests.Validators;

public sealed class RequestMutationBodyValidator : AbstractValidator<RequestMutationBody>
{
    public RequestMutationBodyValidator()
    {
        RuleFor(body => body.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(body => body.Department)
            .MaximumLength(120)
            .When(body => !string.IsNullOrWhiteSpace(body.Department));

        RuleFor(body => body.SponsorshipTypeId)
            .NotEmpty();

        RuleFor(body => body.EventName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(body => body.EventDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date must be today or later.");

        RuleFor(body => body.RequestedAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(RequestValidationConstants.MaxRequestedAmount);

        RuleFor(body => body.Purpose)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(body => body.ExpectedBenefit)
            .MaximumLength(4000)
            .When(body => body.ExpectedBenefit is not null);

        RuleFor(body => body.Remarks)
            .MaximumLength(4000)
            .When(body => body.Remarks is not null);
    }
}
