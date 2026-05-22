using FluentValidation;
using SponsorshipApproval.Application.SponsorshipTypes.Commands;

namespace SponsorshipApproval.Application.SponsorshipTypes.Validators;

public sealed class UpdateSponsorshipTypeCommandValidator : AbstractValidator<UpdateSponsorshipTypeCommand>
{
    public UpdateSponsorshipTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Body).SetValidator(new SponsorshipTypeMutationBodyValidator());
    }
}
