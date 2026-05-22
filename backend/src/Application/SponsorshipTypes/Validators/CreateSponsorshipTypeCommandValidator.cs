using FluentValidation;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;

namespace SponsorshipApproval.Application.SponsorshipTypes.Validators;

public sealed class CreateSponsorshipTypeCommandValidator : AbstractValidator<CreateSponsorshipTypeCommand>
{
    public CreateSponsorshipTypeCommandValidator()
    {
        RuleFor(command => command.Body).SetValidator(new SponsorshipTypeMutationBodyValidator());
    }
}
