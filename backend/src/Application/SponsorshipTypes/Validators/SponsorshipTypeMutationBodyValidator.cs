using FluentValidation;
using SponsorshipApproval.Application.SponsorshipTypes.Models;

namespace SponsorshipApproval.Application.SponsorshipTypes.Validators;

public sealed class SponsorshipTypeMutationBodyValidator : AbstractValidator<SponsorshipTypeMutationBody>
{
    public SponsorshipTypeMutationBodyValidator()
    {
        RuleFor(body => body.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(body => body.Description)
            .MaximumLength(1000)
            .When(body => body.Description is not null);
    }
}
